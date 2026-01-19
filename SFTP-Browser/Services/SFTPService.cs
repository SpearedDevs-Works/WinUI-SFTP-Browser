using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using SFTP_Browser.Models;

#nullable enable

namespace SFTP_Browser.Services;

public sealed class SFTPService : IDisposable
{
    private SftpClient? _client;

    public bool IsConnected => _client?.IsConnected == true;

    public Task ConnectAsync(SFTPConnectionModel model, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            Disconnect();

            _client = SftpClientFactory.CreateAndConnect(model);
        }, cancellationToken);
    }

    public Task<IReadOnlyList<SftpItemModel>> ListDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<SftpItemModel>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var client = _client ?? throw new InvalidOperationException("Not connected.");
            if (!client.IsConnected)
                throw new InvalidOperationException("Not connected.");

            return client.ListDirectory(path)
                .Select(static f => ToModel((SftpFile)f))
                .ToList();
        }, cancellationToken);
    }

    public Task UploadFileAsync(string localFilePath, string remoteDirectory, CancellationToken cancellationToken = default)
        => UploadFileAsync(localFilePath, remoteDirectory, progress: null, cancellationToken);

    public Task UploadFileAsync(string localFilePath, string remoteDirectory, IProgress<double>? progress, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var client = _client ?? throw new InvalidOperationException("Not connected.");
            if (!client.IsConnected)
                throw new InvalidOperationException("Not connected.");

            UploadFileCore(client, localFilePath, remoteDirectory, progress, cancellationToken);
        }, cancellationToken);
    }

    public Task DownloadFileAsync(string remoteFilePath, string localDirectory, CancellationToken cancellationToken = default)
        => DownloadFileAsync(remoteFilePath, localDirectory, progress: null, cancellationToken);

    public Task DownloadFileAsync(string remoteFilePath, string localDirectory, IProgress<double>? progress, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var client = _client ?? throw new InvalidOperationException("Not connected.");
            if (!client.IsConnected)
                throw new InvalidOperationException("Not connected.");

            DownloadFileCore(client, remoteFilePath, localDirectory, progress, cancellationToken);
        }, cancellationToken);
    }

    // For parallel transfer queue: create a dedicated SftpClient per job.
    public static Task UploadFileWithNewClientAsync(SFTPConnectionModel model, string localFilePath, string remoteDirectory, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var client = SftpClientFactory.CreateAndConnect(model);
            UploadFileCore(client, localFilePath, remoteDirectory, progress, cancellationToken);
        }, cancellationToken);
    }

    public static Task DownloadFileWithNewClientAsync(SFTPConnectionModel model, string remoteFilePath, string localDirectory, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var client = SftpClientFactory.CreateAndConnect(model);
            DownloadFileCore(client, remoteFilePath, localDirectory, progress, cancellationToken);
        }, cancellationToken);
    }

    private static void UploadFileCore(SftpClient client, string localFilePath, string remoteDirectory, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(localFilePath))
            throw new ArgumentException("Local file path is required.", nameof(localFilePath));

        if (!File.Exists(localFilePath))
            throw new FileNotFoundException("Local file not found.", localFilePath);

        remoteDirectory = string.IsNullOrWhiteSpace(remoteDirectory) ? "/" : remoteDirectory;

        EnsureRemoteDirectoryExists(client, remoteDirectory);

        var fileName = Path.GetFileName(localFilePath);
        var remotePath = CombineRemotePath(remoteDirectory, fileName);

        using var fs = File.OpenRead(localFilePath);
        var length = fs.Length;

        client.UploadFile(fs, remotePath, canOverride: true, uploadedBytes =>
        {
            if (length > 0)
                progress?.Report((double)uploadedBytes / length);

            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);
        });

        progress?.Report(1);
    }

    private static void DownloadFileCore(SftpClient client, string remoteFilePath, string localDirectory, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(remoteFilePath))
            throw new ArgumentException("Remote file path is required.", nameof(remoteFilePath));

        if (string.IsNullOrWhiteSpace(localDirectory))
            throw new ArgumentException("Local directory is required.", nameof(localDirectory));

        Directory.CreateDirectory(localDirectory);

        var fileName = Path.GetFileName(remoteFilePath);
        var localPath = Path.Combine(localDirectory, fileName);

        // Determine size if possible for progress.
        long length = 0;
        try
        {
            length = client.GetAttributes(remoteFilePath).Size;
        }
        catch
        {
        }

        using var fs = File.Create(localPath);

        client.DownloadFile(remoteFilePath, fs, downloadedBytes =>
        {
            if (length > 0)
                progress?.Report((double)downloadedBytes / length);

            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);
        });

        progress?.Report(1);
    }

    public Task CreateDirectoryAsync(string remoteDirectoryPath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var client = _client ?? throw new InvalidOperationException("Not connected.");
            if (!client.IsConnected)
                throw new InvalidOperationException("Not connected.");

            if (string.IsNullOrWhiteSpace(remoteDirectoryPath))
                throw new ArgumentException("Remote directory path is required.", nameof(remoteDirectoryPath));

            EnsureRemoteDirectoryExists(client, remoteDirectoryPath);
        }, cancellationToken);
    }

    public Task DeletePathAsync(string remotePath, bool isDirectory, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var client = _client ?? throw new InvalidOperationException("Not connected.");
            if (!client.IsConnected)
                throw new InvalidOperationException("Not connected.");

            if (string.IsNullOrWhiteSpace(remotePath))
                throw new ArgumentException("Remote path is required.", nameof(remotePath));

            if (isDirectory)
            {
                DeleteDirectoryRecursive(client, remotePath, cancellationToken);
                return;
            }

            if (client.Exists(remotePath))
                client.DeleteFile(remotePath);
        }, cancellationToken);
    }

    public Task RenameAsync(string remotePath, string newName, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var client = _client ?? throw new InvalidOperationException("Not connected.");
            if (!client.IsConnected)
                throw new InvalidOperationException("Not connected.");

            if (string.IsNullOrWhiteSpace(remotePath))
                throw new ArgumentException("Remote path is required.", nameof(remotePath));
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("New name is required.", nameof(newName));

            var parent = GetRemoteParent(remotePath);
            var newPath = CombineRemotePath(parent, newName);
            client.RenameFile(remotePath, newPath);
        }, cancellationToken);
    }

    private static string GetRemoteParent(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || fullPath == "/")
            return "/";

        var idx = fullPath.LastIndexOf('/', fullPath.Length - 1);
        if (idx <= 0)
            return "/";

        return fullPath[..idx];
    }

    private static void DeleteDirectoryRecursive(SftpClient client, string directory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var entry in client.ListDirectory(directory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.Name is "." or "..")
                continue;

            if (entry.IsDirectory)
            {
                DeleteDirectoryRecursive(client, entry.FullName, cancellationToken);
            }
            else
            {
                client.DeleteFile(entry.FullName);
            }
        }

        client.DeleteDirectory(directory);
    }

    public void Disconnect()
    {
        try
        {
            if (_client is { IsConnected: true })
                _client.Disconnect();
        }
        catch
        {
        }
        finally
        {
            _client?.Dispose();
            _client = null;
        }
    }

    private static SftpItemModel ToModel(SftpFile f)
    {
        return new SftpItemModel
        {
            Name = f.Name,
            FullPath = f.FullName,
            IsDirectory = f.IsDirectory,
            Length = f.IsDirectory ? 0 : f.Length,
            LastWriteTime = f.LastWriteTimeUtc == default
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(f.LastWriteTimeUtc, DateTimeKind.Utc))
        };
    }

    private static string CombineRemotePath(string directory, string name)
    {
        if (directory == "/")
            return "/" + name;
        if (directory.EndsWith("/", StringComparison.Ordinal))
            return directory + name;
        return directory + "/" + name;
    }

    private static void EnsureRemoteDirectoryExists(SftpClient client, string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || directory == "/")
            return;

        var parts = directory.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        var current = "/";
        foreach (var part in parts)
        {
            current = CombineRemotePath(current, part);
            if (!client.Exists(current))
                client.CreateDirectory(current);
        }
    }

    public void Dispose() => Disconnect();
}
