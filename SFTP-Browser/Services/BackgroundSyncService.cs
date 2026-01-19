using System;
using System.Threading;
using System.Threading.Tasks;
using SFTP_Browser.Models;
using SFTP_Browser.ViewModels;

#nullable enable

namespace SFTP_Browser.Services;

public sealed class BackgroundSyncService
{
    private readonly TransferQueueService _queue;
    private readonly SyncPlannerService _planner = new();

    public BackgroundSyncService(TransferQueueService queue)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    public Task<bool> HasConflictsAsync(
        SFTPConnectionModel connection,
        string remoteRoot,
        string localRoot,
        CancellationToken cancellationToken = default)
        => _planner.HasConflictsAsync(connection, remoteRoot, localRoot, cancellationToken);

    public async Task QueueBiDirectionalSyncAsync(
        SFTPConnectionModel connection,
        string remoteRoot,
        string localRoot,
        SyncConflictMode conflictMode,
        Action<TransferItemViewModel> onNewTransfer,
        CancellationToken cancellationToken = default)
    {
        if (onNewTransfer is null)
            throw new ArgumentNullException(nameof(onNewTransfer));

        var plan = await _planner.PlanBiDirectionalSyncAsync(connection, remoteRoot, localRoot, conflictMode, cancellationToken);

        foreach (var item in plan)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (item.Decision)
            {
                case SyncPlannerService.SyncDecision.Download:
                {
                    var localDir = System.IO.Path.GetDirectoryName(item.LocalPath) ?? localRoot;

                    var model = new TransferItemModel
                    {
                        Direction = TransferDirection.Download,
                        SourcePath = item.RemotePath,
                        DestinationPath = localDir,
                    };

                    var vm = new TransferItemViewModel(model);
                    onNewTransfer(vm);

                    _queue.Enqueue(vm, (ct, progress) =>
                        SFTPService.DownloadFileWithNewClientAsync(connection, item.RemotePath, localDir, progress, ct));
                    break;
                }
                case SyncPlannerService.SyncDecision.Upload:
                {
                    var remoteDir = GetRemoteParent(item.RemotePath);

                    var model = new TransferItemModel
                    {
                        Direction = TransferDirection.Upload,
                        SourcePath = item.LocalPath,
                        DestinationPath = remoteDir,
                    };

                    var vm = new TransferItemViewModel(model);
                    onNewTransfer(vm);

                    _queue.Enqueue(vm, (ct, progress) =>
                        SFTPService.UploadFileWithNewClientAsync(connection, item.LocalPath, remoteDir, progress, ct));
                    break;
                }
                default:
                    break;
            }
        }
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

    public async Task QueueRecursiveSyncAsync(
        SFTPConnectionModel connection,
        string remoteRoot,
        string localRoot,
        SyncConflictMode conflictMode,
        Action<TransferItemViewModel> onNewTransfer,
        CancellationToken cancellationToken = default)
    {
        if (onNewTransfer is null)
            throw new ArgumentNullException(nameof(onNewTransfer));

        var plan = await _planner.PlanDownloadSyncAsync(connection, remoteRoot, localRoot, conflictMode, cancellationToken);

        foreach (var item in plan)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item.Decision != SyncPlannerService.SyncDecision.Download)
                continue;

            var localDir = System.IO.Path.GetDirectoryName(item.LocalPath) ?? localRoot;

            var model = new TransferItemModel
            {
                Direction = TransferDirection.Download,
                SourcePath = item.RemotePath,
                DestinationPath = localDir,
            };

            var vm = new TransferItemViewModel(model);
            onNewTransfer(vm);

            _queue.Enqueue(vm, (ct, progress) =>
                SFTPService.DownloadFileWithNewClientAsync(connection, item.RemotePath, localDir, progress, ct));
        }
    }
}
