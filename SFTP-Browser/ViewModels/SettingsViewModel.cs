using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Octokit;
using SFTP_Browser.Models;
using SFTP_Browser.Services;
using Windows.ApplicationModel;

#nullable enable

namespace SFTP_Browser.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService = App.SettingsService;
    private readonly UpdateCheckService _updateCheckService = new();
    private Release? _latestRelease;
    private string? _downloadedInstallerPath;

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

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private bool _isCheckingForUpdates;

    [ObservableProperty]
    private string? _updateCheckMessage;

    [ObservableProperty]
    private bool _isDownloadingUpdate;

    [ObservableProperty]
    private int _updateDownloadProgress;

    [ObservableProperty]
    private bool _updateAvailable;

    [ObservableProperty]
    private bool _updateDownloaded;

    public SettingsViewModel()
    {
        _updateCheckService.DownloadProgressChanged += UpdateCheckService_DownloadProgressChanged;
    }

    private void UpdateCheckService_DownloadProgressChanged(object? sender, UpdateProgressEventArgs e)
    {
        UpdateDownloadProgress = e.ProgressPercentage;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken);

        NotificationsEnabled = settings.NotificationsEnabled;

        BackgroundSyncEnabled = settings.BackgroundSync.Enabled;
        BackgroundSyncRemoteFolder = settings.BackgroundSync.RemoteFolder;
        BackgroundSyncLocalFolder = settings.BackgroundSync.LocalFolder;
        BackgroundSyncIntervalMinutes = Math.Max(1, settings.BackgroundSync.Interval.TotalMinutes);
        BackgroundSyncConflictSelectedIndex = settings.BackgroundSync.ConflictMode == SyncConflictMode.Overwrite ? 1 : 0;

        var version = global::Windows.ApplicationModel.Package.Current.Id.Version;
        AppVersion = $"{version.Major}.{version.Minor}.{version.Build}";
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

    [RelayCommand]
    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        IsCheckingForUpdates = true;
        UpdateCheckMessage = null;
        UpdateAvailable = false;
        UpdateDownloaded = false;

        try
        {
            var currentVersion = AppVersion.Split('-')[0];
            var latestRelease = await _updateCheckService.CheckForUpdatesAsync(currentVersion, cancellationToken);

            if (latestRelease != null)
            {
                _latestRelease = latestRelease;
                UpdateAvailable = true;
                UpdateCheckMessage = $"New version available: {latestRelease.TagName}";
            }
            else
            {
                UpdateCheckMessage = "You are running the latest version.";
            }
        }
        catch (Exception ex)
        {
            UpdateCheckMessage = $"Error checking for updates: {ex.Message}";
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    [RelayCommand]
    public async Task DownloadAndInstallUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (_latestRelease == null)
        {
            UpdateCheckMessage = "No update available to download.";
            return;
        }

        IsDownloadingUpdate = true;
        UpdateDownloadProgress = 0;
        UpdateCheckMessage = "Downloading update...";

        try
        {
            // Download the update
            var installerPath = await _updateCheckService.DownloadReleaseAsync(_latestRelease, cancellationToken);

            if (installerPath == null)
            {
                UpdateCheckMessage = "Failed to download update. Please try again later.";
                return;
            }

            _downloadedInstallerPath = installerPath;
            UpdateDownloaded = true;
            UpdateCheckMessage = $"Update downloaded. Click 'Install Update' to install version {_latestRelease.TagName}.";
        }
        catch (Exception ex)
        {
            UpdateCheckMessage = $"Error downloading update: {ex.Message}";
        }
        finally
        {
            IsDownloadingUpdate = false;
        }
    }

    [RelayCommand]
    public async Task InstallUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (_downloadedInstallerPath == null || !System.IO.File.Exists(_downloadedInstallerPath))
        {
            UpdateCheckMessage = "Installer file not found. Please download again.";
            return;
        }

        UpdateCheckMessage = "Installing update. The application will restart...";

        try
        {
            var success = await _updateCheckService.InstallUpdateAsync(_downloadedInstallerPath, cancellationToken);

            if (success)
            {
                UpdateCheckMessage = "Update installed successfully. Please restart the application.";
                UpdateDownloaded = false;
            }
            else
            {
                UpdateCheckMessage = "Update installation completed. Please restart the application manually.";
            }
        }
        catch (Exception ex)
        {
            UpdateCheckMessage = $"Error installing update: {ex.Message}";
        }
    }

    public void CleanupOldUpdates()
    {
        _updateCheckService.CleanupDownloads();
    }
}
