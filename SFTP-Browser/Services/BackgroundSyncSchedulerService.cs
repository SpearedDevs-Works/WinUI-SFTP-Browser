using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SFTP_Browser.Models;

#nullable enable

namespace SFTP_Browser.Services;

public sealed class BackgroundSyncSchedulerService : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly NotificationService _notificationService;
    private readonly CredentialStoreService _credentialStore = new();

    private CancellationTokenSource? _cts;
    private Task? _loop;

    public BackgroundSyncSchedulerService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _notificationService = new NotificationService(settingsService);
    }

    public void Start(CancellationToken cancellationToken = default)
    {
        Stop();

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cts.Token;

        _loop = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                AppSettingsModel settings;
                try
                {
                    settings = await _settingsService.LoadAsync(token);
                }
                catch
                {
                    settings = new AppSettingsModel();
                }

                if (!settings.BackgroundSync.Enabled)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                    continue;
                }

                var interval = settings.BackgroundSync.Interval <= TimeSpan.Zero
                    ? TimeSpan.FromMinutes(30)
                    : settings.BackgroundSync.Interval;

                var now = DateTimeOffset.UtcNow;
                var last = settings.BackgroundSync.LastRunUtc;
                var dueIn = last is null ? TimeSpan.Zero : (last.Value + interval) - now;
                if (dueIn > TimeSpan.Zero)
                    await Task.Delay(dueIn, token);

                if (token.IsCancellationRequested)
                    break;

                try
                {
                    await RunOnceAsync(settings, token);

                    settings.BackgroundSync.LastRunUtc = DateTimeOffset.UtcNow;
                    await _settingsService.SaveAsync(settings, token);

                    await _notificationService.NotifyAsync("Background sync", "Sync completed.", token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await _notificationService.NotifyAsync("Background sync", $"Sync failed: {ex.Message}", token);
                }

                await Task.Delay(interval, token);
            }
        }, token);
    }

    private async Task RunOnceAsync(AppSettingsModel settings, CancellationToken token)
    {
        if (settings.RecentConnections.Count == 0)
            throw new InvalidOperationException("No recent connections available.");

        var rc = settings.RecentConnections[0];

        if (string.IsNullOrWhiteSpace(settings.BackgroundSync.LocalFolder))
            throw new InvalidOperationException("Background sync local folder is not configured.");

        Directory.CreateDirectory(settings.BackgroundSync.LocalFolder);

        var password = await _credentialStore.TryGetPasswordAsync(rc.Host, rc.Port, rc.Username, token);
        if (password is null)
            throw new InvalidOperationException("No saved credentials found in Windows Credential Manager.");

        var model = new SFTPConnectionModel
        {
            Host = rc.Host,
            Port = rc.Port,
            Username = rc.Username,
            Password = password,
            AuthenticationMode = SftpAuthenticationMode.Password,
            InitialPath = string.IsNullOrWhiteSpace(settings.BackgroundSync.RemoteFolder) ? "/" : settings.BackgroundSync.RemoteFolder,
        };

        using var sftp = new SFTPService();
        await sftp.ConnectAsync(model, token);

        var planner = new SyncPlannerService();
        var plan = await planner.PlanDownloadSyncAsync(
            model,
            remoteRoot: model.InitialPath,
            localRoot: settings.BackgroundSync.LocalFolder,
            conflictMode: settings.BackgroundSync.ConflictMode,
            cancellationToken: token);

        foreach (var item in plan)
        {
            token.ThrowIfCancellationRequested();

            if (item.Decision != SyncPlannerService.SyncDecision.Download)
                continue;

            var localDir = Path.GetDirectoryName(item.LocalPath) ?? settings.BackgroundSync.LocalFolder;
            Directory.CreateDirectory(localDir);

            await SFTPService.DownloadFileWithNewClientAsync(
                model,
                item.RemotePath,
                localDir,
                progress: null,
                token);
        }

        sftp.Disconnect();
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        _cts?.Dispose();
        _cts = null;
        _loop = null;
    }

    public void Dispose() => Stop();
}
