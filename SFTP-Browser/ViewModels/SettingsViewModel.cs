using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SFTP_Browser.Models;
using SFTP_Browser.Services;

#nullable enable

namespace SFTP_Browser.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService = App.SettingsService;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private bool _backgroundSyncEnabled;

    [ObservableProperty]
    private string _backgroundSyncRemoteFolder = "/";

    [ObservableProperty]
    private string _backgroundSyncLocalFolder = "";

    [ObservableProperty]
    private double _backgroundSyncIntervalMinutes = 30;

    [ObservableProperty]
    private int _backgroundSyncConflictSelectedIndex;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken);

        NotificationsEnabled = settings.NotificationsEnabled;

        BackgroundSyncEnabled = settings.BackgroundSync.Enabled;
        BackgroundSyncRemoteFolder = settings.BackgroundSync.RemoteFolder;
        BackgroundSyncLocalFolder = settings.BackgroundSync.LocalFolder;
        BackgroundSyncIntervalMinutes = Math.Max(1, settings.BackgroundSync.Interval.TotalMinutes);
        BackgroundSyncConflictSelectedIndex = settings.BackgroundSync.ConflictMode == SyncConflictMode.Overwrite ? 1 : 0;
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken);

        settings.NotificationsEnabled = NotificationsEnabled;

        settings.BackgroundSync.Enabled = BackgroundSyncEnabled;
        settings.BackgroundSync.RemoteFolder = string.IsNullOrWhiteSpace(BackgroundSyncRemoteFolder) ? "/" : BackgroundSyncRemoteFolder;
        settings.BackgroundSync.LocalFolder = BackgroundSyncLocalFolder ?? "";

        var minutes = Math.Max(1, BackgroundSyncIntervalMinutes);
        settings.BackgroundSync.Interval = TimeSpan.FromMinutes(minutes);

        settings.BackgroundSync.ConflictMode = BackgroundSyncConflictSelectedIndex == 1
            ? SyncConflictMode.Overwrite
            : SyncConflictMode.Skip;

        await _settingsService.SaveAsync(settings, cancellationToken);
    }
}
