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

public sealed class SyncPlannerService
{
    public async Task<IReadOnlyList<PlannedSyncItem>> PlanDownloadSyncAsync(
        SFTPConnectionModel connection,
        string remoteRoot,
        string localRoot,
        SyncConflictMode conflictMode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(remoteRoot))
            throw new ArgumentException("Remote root is required.", nameof(remoteRoot));
        if (string.IsNullOrWhiteSpace(localRoot))
            throw new ArgumentException("Local root is required.", nameof(localRoot));

        remoteRoot = NormalizeRemoteRoot(remoteRoot);
        localRoot = Path.GetFullPath(localRoot);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var client = SftpClientFactory.CreateAndConnect(connection);

            var plan = new List<PlannedSyncItem>();
            WalkRemoteForDownloadPlan(client, remoteRoot, localRoot, remoteRoot, conflictMode, plan, cancellationToken);
            return (IReadOnlyList<PlannedSyncItem>)plan;
        }, cancellationToken);
    }

    public async Task<bool> HasConflictsAsync(
        SFTPConnectionModel connection,
        string remoteRoot,
        string localRoot,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(remoteRoot))
            throw new ArgumentException("Remote root is required.", nameof(remoteRoot));
        if (string.IsNullOrWhiteSpace(localRoot))
            throw new ArgumentException("Local root is required.", nameof(localRoot));

        remoteRoot = NormalizeRemoteRoot(remoteRoot);
        localRoot = Path.GetFullPath(localRoot);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var client = SftpClientFactory.CreateAndConnect(connection);
            return WalkRemoteForConflicts(client, remoteRoot, localRoot, remoteRoot, cancellationToken);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<PlannedSyncItem>> PlanBiDirectionalSyncAsync(
        SFTPConnectionModel connection,
        string remoteRoot,
        string localRoot,
        SyncConflictMode conflictMode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(remoteRoot))
            throw new ArgumentException("Remote root is required.", nameof(remoteRoot));
        if (string.IsNullOrWhiteSpace(localRoot))
            throw new ArgumentException("Local root is required.", nameof(localRoot));

        remoteRoot = NormalizeRemoteRoot(remoteRoot);
        localRoot = Path.GetFullPath(localRoot);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var client = SftpClientFactory.CreateAndConnect(connection);

            var remoteFiles = new Dictionary<string, RemoteFileInfo>(StringComparer.OrdinalIgnoreCase);
            CollectRemoteFiles(client, remoteRoot, remoteRoot, remoteFiles, cancellationToken);

            var localFiles = new Dictionary<string, LocalFileInfo>(StringComparer.OrdinalIgnoreCase);
            CollectLocalFiles(localRoot, localRoot, localFiles, cancellationToken);

            var plan = new List<PlannedSyncItem>();

            foreach (var (rel, r) in remoteFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var localPath = Path.Combine(localRoot, rel.Replace('/', Path.DirectorySeparatorChar));

                if (!localFiles.TryGetValue(rel, out var l))
                {
                    plan.Add(new PlannedSyncItem(r.RemotePath, localPath, SyncDecision.Download));
                    continue;
                }

                if (DecideDownload(r, l, conflictMode))
                    plan.Add(new PlannedSyncItem(r.RemotePath, localPath, SyncDecision.Download));
            }

            foreach (var (rel, l) in localFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var remotePath = CombineRemotePath(remoteRoot, rel);

                if (!remoteFiles.TryGetValue(rel, out var r))
                {
                    plan.Add(new PlannedSyncItem(remotePath, l.LocalPath, SyncDecision.Upload));
                    continue;
                }

                if (DecideUpload(l, r, conflictMode))
                    plan.Add(new PlannedSyncItem(remotePath, l.LocalPath, SyncDecision.Upload));
            }

            return (IReadOnlyList<PlannedSyncItem>)plan;
        }, cancellationToken);
    }

    private static bool WalkRemoteForConflicts(
        SftpClient client,
        string currentRemote,
        string localRoot,
        string remoteRoot,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var entry in client.ListDirectory(currentRemote))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.Name is "." or "..")
                continue;

            if (entry.IsDirectory)
            {
                if (WalkRemoteForConflicts(client, entry.FullName, localRoot, remoteRoot, cancellationToken))
                    return true;

                continue;
            }

            var rel = GetRelativeRemotePath(remoteRoot, entry.FullName);
            var localPath = Path.Combine(localRoot, rel.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(localPath))
                return true;
        }

        return false;
    }

    private static void WalkRemoteForDownloadPlan(
        SftpClient client,
        string currentRemote,
        string localRoot,
        string remoteRoot,
        SyncConflictMode conflictMode,
        List<PlannedSyncItem> plan,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var entry in client.ListDirectory(currentRemote))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.Name is "." or "..")
                continue;

            if (entry.IsDirectory)
            {
                WalkRemoteForDownloadPlan(client, entry.FullName, localRoot, remoteRoot, conflictMode, plan, cancellationToken);
                continue;
            }

            var rel = GetRelativeRemotePath(remoteRoot, entry.FullName);
            var localPath = Path.Combine(localRoot, rel.Replace('/', Path.DirectorySeparatorChar));

            var decision = DecideDownloadOnly(entry, localPath, conflictMode);
            if (decision is null)
                continue;

            plan.Add(new PlannedSyncItem(entry.FullName, localPath, decision.Value));
        }
    }

    private static SyncDecision? DecideDownloadOnly(ISftpFile entry, string localPath, SyncConflictMode conflictMode)
    {
        if (!File.Exists(localPath))
            return SyncDecision.Download;

        if (conflictMode == SyncConflictMode.Overwrite)
            return SyncDecision.Download;

        try
        {
            var fi = new FileInfo(localPath);
            if (fi.Length != entry.Length)
                return SyncDecision.Download;

            var localUtc = fi.LastWriteTimeUtc;
            var remoteUtc = entry.LastWriteTimeUtc;
            if (remoteUtc > localUtc.AddSeconds(2))
                return SyncDecision.Download;
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static bool DecideDownload(RemoteFileInfo remote, LocalFileInfo local, SyncConflictMode conflictMode)
    {
        if (conflictMode == SyncConflictMode.Overwrite)
            return true;

        if (remote.Length != local.Length)
            return true;

        if (remote.LastWriteTimeUtc > local.LastWriteTimeUtc.AddSeconds(2))
            return true;

        return false;
    }

    private static bool DecideUpload(LocalFileInfo local, RemoteFileInfo remote, SyncConflictMode conflictMode)
    {
        if (conflictMode == SyncConflictMode.Overwrite)
            return true;

        if (local.Length != remote.Length)
            return true;

        if (local.LastWriteTimeUtc > remote.LastWriteTimeUtc.AddSeconds(2))
            return true;

        return false;
    }

    private static void CollectRemoteFiles(
        SftpClient client,
        string currentRemote,
        string remoteRoot,
        Dictionary<string, RemoteFileInfo> files,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var entry in client.ListDirectory(currentRemote))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.Name is "." or "..")
                continue;

            if (entry.IsDirectory)
            {
                CollectRemoteFiles(client, entry.FullName, remoteRoot, files, cancellationToken);
                continue;
            }

            var rel = GetRelativeRemotePath(remoteRoot, entry.FullName);
            files[rel] = new RemoteFileInfo(entry.FullName, entry.Length, entry.LastWriteTimeUtc);
        }
    }

    private static void CollectLocalFiles(
        string currentLocal,
        string localRoot,
        Dictionary<string, LocalFileInfo> files,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var file in Directory.EnumerateFiles(currentLocal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rel = Path.GetRelativePath(localRoot, file)
                .Replace(Path.DirectorySeparatorChar, '/');

            var fi = new FileInfo(file);
            files[rel] = new LocalFileInfo(file, fi.Length, fi.LastWriteTimeUtc);
        }

        foreach (var dir in Directory.EnumerateDirectories(currentLocal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            CollectLocalFiles(dir, localRoot, files, cancellationToken);
        }
    }

    private static string CombineRemotePath(string remoteRoot, string relative)
    {
        relative = relative.Replace('\\', '/').TrimStart('/');

        if (string.Equals(remoteRoot, "/", StringComparison.Ordinal))
            return "/" + relative;

        return remoteRoot + "/" + relative;
    }

    private static string NormalizeRemoteRoot(string remoteRoot)
    {
        remoteRoot = remoteRoot.Trim();
        if (remoteRoot.Length == 0)
            return "/";
        if (!remoteRoot.StartsWith("/", StringComparison.Ordinal))
            remoteRoot = "/" + remoteRoot;
        if (remoteRoot.Length > 1 && remoteRoot.EndsWith("/", StringComparison.Ordinal))
            remoteRoot = remoteRoot.TrimEnd('/');
        return remoteRoot;
    }

    private static string GetRelativeRemotePath(string remoteRoot, string fullRemotePath)
    {
        if (string.Equals(remoteRoot, "/", StringComparison.Ordinal))
            return fullRemotePath.TrimStart('/');

        if (fullRemotePath.StartsWith(remoteRoot + "/", StringComparison.Ordinal))
            return fullRemotePath[(remoteRoot.Length + 1)..];

        // Fallback: best effort
        return Path.GetFileName(fullRemotePath);
    }

    private readonly record struct RemoteFileInfo(string RemotePath, long Length, DateTime LastWriteTimeUtc);

    private readonly record struct LocalFileInfo(string LocalPath, long Length, DateTime LastWriteTimeUtc);

    public enum SyncDecision
    {
        Download,
        Upload,
    }

    public sealed record PlannedSyncItem(string RemotePath, string LocalPath, SyncDecision Decision);
}
