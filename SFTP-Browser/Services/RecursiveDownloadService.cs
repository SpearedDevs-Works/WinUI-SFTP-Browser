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

public sealed class RecursiveDownloadService
{
    public async Task<IReadOnlyList<(string RemoteFilePath, string LocalDirectory)>> ExpandToFileDownloadsAsync(
        SFTPConnectionModel connection,
        IEnumerable<SftpItemModel> selectedItems,
        string localRoot,
        SyncConflictMode conflictMode,
        CancellationToken cancellationToken)
    {
        if (selectedItems is null)
            throw new ArgumentNullException(nameof(selectedItems));
        if (string.IsNullOrWhiteSpace(localRoot))
            throw new ArgumentException("Local root is required.", nameof(localRoot));

        localRoot = Path.GetFullPath(localRoot);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var client = SftpClientFactory.CreateAndConnect(connection);
            var results = new List<(string RemoteFilePath, string LocalDirectory)>();

            foreach (var item in selectedItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!item.IsDirectory)
                {
                    if (ShouldDownloadFile(client, item.FullPath, Path.Combine(localRoot, item.Name), conflictMode))
                        results.Add((item.FullPath, localRoot));
                    continue;
                }

                var baseFolder = Path.Combine(localRoot, item.Name);
                ExpandDirectory(client, item.FullPath, baseFolder, conflictMode, results, cancellationToken);
            }

            return (IReadOnlyList<(string RemoteFilePath, string LocalDirectory)>)results;
        }, cancellationToken);
    }

    private static void ExpandDirectory(
        SftpClient client,
        string remoteDir,
        string localDir,
        SyncConflictMode conflictMode,
        List<(string RemoteFilePath, string LocalDirectory)> results,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(localDir);

        foreach (var entry in client.ListDirectory(remoteDir))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.Name is "." or "..")
                continue;

            if (entry.IsDirectory)
            {
                ExpandDirectory(client, entry.FullName, Path.Combine(localDir, entry.Name), conflictMode, results, cancellationToken);
                continue;
            }

            var localPath = Path.Combine(localDir, entry.Name);
            if (ShouldDownloadFile(client, entry.FullName, localPath, conflictMode))
                results.Add((entry.FullName, localDir));
        }
    }

    private static bool ShouldDownloadFile(SftpClient client, string remoteFile, string localPath, SyncConflictMode conflictMode)
    {
        if (!File.Exists(localPath))
            return true;

        if (conflictMode == SyncConflictMode.Overwrite)
            return true;

        try
        {
            var remoteAttr = client.GetAttributes(remoteFile);
            var fi = new FileInfo(localPath);

            if (fi.Length != remoteAttr.Size)
                return true;

            var localUtc = fi.LastWriteTimeUtc;
            var remoteUtc = remoteAttr.LastWriteTimeUtc;
            return remoteUtc > localUtc.AddSeconds(2);
        }
        catch
        {
            return false;
        }
    }
}
