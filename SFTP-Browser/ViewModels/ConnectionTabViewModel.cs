using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using SFTP_Browser.Models;
using SFTP_Browser.Services;

#nullable enable

namespace SFTP_Browser.ViewModels;

public sealed partial class ConnectionTabViewModel : ObservableObject
{
    private readonly SFTPService _sftpService;
    private readonly SettingsService _settingsService = new();
    private readonly TransferQueueService _transferQueue;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private SFTPConnectionModel? _activeConnection;

    public ObservableCollection<TransferItemViewModel> Transfers { get; } = new();

    private List<FileItemViewModel> _allItems = new();

    private readonly Stack<string> _back = new();
    private readonly Stack<string> _forward = new();

    private readonly BackgroundSyncService _backgroundSync;
    private readonly RecursiveDownloadService _recursiveDownload = new();
    private readonly CredentialStoreService _credentialStore = new();

    public ConnectionTabViewModel(DispatcherQueue dispatcherQueue)
    {
        _sftpService = new SFTPService();
        _transferQueue = new TransferQueueService(dispatcherQueue, maxConcurrency: 6);
        _backgroundSync = new BackgroundSyncService(_transferQueue);

        CurrentPath = "/";
        StatusText = "Disconnected";
        ConnectionStatus = "Not connected";
        ShowEmptyState = true;

        SortMode = nameof(FileSortMode.Name);
        TabTitle = "New SFTP Connection";
    }

    public ConnectionTabViewModel() : this(DispatcherQueue.GetForCurrentThread())
    {
    }

    [ObservableProperty]
    private string _tabTitle = "New SFTP Connection";

    [ObservableProperty]
    private string _searchText = "";

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilterAndSort();
    }

    [ObservableProperty]
    private string _sortMode = "Name";

    partial void OnSortModeChanged(string value)
    {
        ApplyFilterAndSort();
    }

    [ObservableProperty]
    private bool _sortDescending;

    partial void OnSortDescendingChanged(bool value)
    {
        ApplyFilterAndSort();
    }

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _currentPath = "/";

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _connectionStatus = "";

    [ObservableProperty]
    private bool _showEmptyState;

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoForward;

    public ObservableCollection<FileItemViewModel> Items { get; } = new();

    [RelayCommand]
    public async Task ConnectWithPasswordAsync((string Host, int Port, string Username, string Password) input)
    {
        var model = new SFTPConnectionModel
        {
            Host = input.Host,
            Port = input.Port,
            Username = input.Username,
            Password = input.Password,
            AuthenticationMode = SftpAuthenticationMode.Password,
            InitialPath = "/"
        };

        await ConnectAsync(model);
    }

    [RelayCommand]
    public async Task ConnectWithPrivateKeyAsync((string Host, int Port, string Username, string PrivateKeyPath, string? Passphrase) input)
    {
        var model = new SFTPConnectionModel
        {
            Host = input.Host,
            Port = input.Port,
            Username = input.Username,
            PrivateKeyPath = input.PrivateKeyPath,
            PrivateKeyPassphrase = input.Passphrase,
            AuthenticationMode = SftpAuthenticationMode.PrivateKey,
            InitialPath = "/"
        };

        await ConnectAsync(model);
    }

    public async Task ConnectAsync(SFTPConnectionModel model, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            IsLoading = true;
            StatusText = "Connecting...";
            await _sftpService.ConnectAsync(model, cancellationToken);
            IsConnected = true;
            ConnectionStatus = $"Connected: {model}";
            TabTitle = model.ToString();
            ShowEmptyState = false;

            _activeConnection = model;

            if (model.AuthenticationMode == SftpAuthenticationMode.Password && !string.IsNullOrEmpty(model.Password))
            {
                try
                {
                    await _credentialStore.SavePasswordAsync(model.Host, model.Port, model.Username, model.Password, cancellationToken);
                }
                catch
                {
                    // best-effort (credential manager might be unavailable)
                }
            }

            await AddToRecentsAsync(model, cancellationToken);

            CurrentPath = string.IsNullOrWhiteSpace(model.InitialPath) ? "/" : model.InitialPath;
            await RefreshCoreAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            IsConnected = false;
            ConnectionStatus = "Not connected";
            TabTitle = "New SFTP Connection";
            ShowEmptyState = true;
            StatusText = ex.Message;
            _activeConnection = null;
            _sftpService.Disconnect();
        }
        finally
        {
            IsLoading = false;
            _gate.Release();
        }
    }

    private async Task AddToRecentsAsync(SFTPConnectionModel model, CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            var key = $"{model.Username}@{model.Host}:{model.Port}";

            settings.RecentConnections.RemoveAll(r => $"{r.Username}@{r.Host}:{r.Port}" == key);
            settings.RecentConnections.Insert(0, new SftpRecentConnectionModel
            {
                Host = model.Host,
                Port = model.Port,
                Username = model.Username,
                LastUsedUtc = DateTimeOffset.UtcNow
            });

            if (settings.RecentConnections.Count > settings.MaxRecentConnections)
                settings.RecentConnections.RemoveRange(settings.MaxRecentConnections, settings.RecentConnections.Count - settings.MaxRecentConnections);

            await _settingsService.SaveAsync(settings, cancellationToken);
        }
        catch
        {
            // ignore settings persistence errors
        }
    }

    [RelayCommand]
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await RefreshCoreAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task RefreshCoreAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        StatusText = "Loading...";

        try
        {
            var entries = await _sftpService.ListDirectoryAsync(CurrentPath, cancellationToken);
            _allItems = entries
                .Where(e => e.Name is not "." and not "..")
                .Select(e => new FileItemViewModel(e))
                .ToList();

            ApplyFilterAndSort();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilterAndSort()
    {
        IEnumerable<FileItemViewModel> filtered = _allItems;

        var term = SearchText?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            filtered = filtered.Where(i => i.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase));
        }

        // Always keep folders first (like Explorer)
        IOrderedEnumerable<FileItemViewModel> ordered = filtered.OrderByDescending(i => i.IsDirectory);

        var descending = SortDescending;
        ordered = SortMode switch
        {
            "Date Modified" => descending
                ? ordered.ThenByDescending(i => i.LastWriteTime ?? DateTimeOffset.MinValue)
                : ordered.ThenBy(i => i.LastWriteTime ?? DateTimeOffset.MinValue),
            "Type" => descending
                ? ordered.ThenByDescending(i => i.Type, StringComparer.CurrentCultureIgnoreCase)
                : ordered.ThenBy(i => i.Type, StringComparer.CurrentCultureIgnoreCase),
            "Size" => descending
                ? ordered.ThenByDescending(i => i.SizeBytes)
                : ordered.ThenBy(i => i.SizeBytes),
            _ => descending
                ? ordered.ThenByDescending(i => i.Name, StringComparer.CurrentCultureIgnoreCase)
                : ordered.ThenBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase),
        };

        var list = ordered.ToList();
        Items.Clear();
        foreach (var vm in list)
            Items.Add(vm);

        StatusText = $"{Items.Count} item(s)";

        HasSelection = false;
    }

    [RelayCommand]
    public Task DisconnectAsync()
    {
        _sftpService.Disconnect();
        _activeConnection = null;

        _back.Clear();
        _forward.Clear();
        UpdateHistoryFlags();

        Items.Clear();
        _allItems.Clear();
        Transfers.Clear();
        _transferQueue.Clear();
        IsConnected = false;
        ShowEmptyState = true;
        StatusText = "Disconnected";
        ConnectionStatus = "Not connected";
        CurrentPath = "/";
        TabTitle = "New SFTP Connection";
        return Task.CompletedTask;
    }

    private void UpdateHistoryFlags()
    {
        CanGoBack = _back.Count > 0;
        CanGoForward = _forward.Count > 0;
    }

    public async Task NavigateToAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return;

        if (!string.Equals(CurrentPath, path, StringComparison.Ordinal))
        {
            _back.Push(CurrentPath);
            _forward.Clear();
            UpdateHistoryFlags();
        }

        CurrentPath = path;
        await RefreshAsync(cancellationToken);
    }

    public async Task NavigateBackAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return;

        if (_back.Count == 0)
            return;

        _forward.Push(CurrentPath);
        var path = _back.Pop();
        UpdateHistoryFlags();

        CurrentPath = path;
        await RefreshAsync(cancellationToken);
    }

    public async Task NavigateForwardAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return;

        if (_forward.Count == 0)
            return;

        _back.Push(CurrentPath);
        var path = _forward.Pop();
        UpdateHistoryFlags();

        CurrentPath = path;
        await RefreshAsync(cancellationToken);
    }

    public void UpdateSelectionCount(int count) => HasSelection = count > 0;

    public async Task UploadFilesAsync(string[] localFilePaths, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return;

        if (_activeConnection is null)
            return;

        if (localFilePaths is null || localFilePaths.Length == 0)
            return;

        foreach (var file in localFilePaths)
        {
            var remoteDest = CurrentPath;
            var model = new TransferItemModel
            {
                Direction = TransferDirection.Upload,
                SourcePath = file,
                DestinationPath = remoteDest,
            };

            var vm = new TransferItemViewModel(model);
            Transfers.Insert(0, vm);

            var connection = _activeConnection;
            _transferQueue.Enqueue(vm, (ct, progress) =>
                SFTPService.UploadFileWithNewClientAsync(connection, file, remoteDest, progress, ct));
        }

        StatusText = $"Queued {localFilePaths.Length} upload(s)";
        await Task.CompletedTask;
    }

    public async Task DownloadItemsAsync(IEnumerable<FileItemViewModel> selectedItems, string localDirectory, SyncConflictMode conflictMode, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return;

        if (_activeConnection is null)
            return;

        if (selectedItems is null)
            return;

        if (string.IsNullOrWhiteSpace(localDirectory))
            return;

        Directory.CreateDirectory(localDirectory);

        // Expand folders to file downloads (recursive) and apply conflict mode.
        var models = selectedItems.Select(i => new SftpItemModel
        {
            Name = i.Name,
            FullPath = i.FullPath,
            IsDirectory = i.IsDirectory,
            Length = i.SizeBytes,
            LastWriteTime = i.LastWriteTime,
        }).ToArray();

        var expanded = await _recursiveDownload.ExpandToFileDownloadsAsync(_activeConnection, models, localDirectory, conflictMode, cancellationToken);
        if (expanded.Count == 0)
        {
            StatusText = "Nothing to download.";
            return;
        }

        foreach (var (remoteFilePath, localDir) in expanded)
        {
            var model = new TransferItemModel
            {
                Direction = TransferDirection.Download,
                SourcePath = remoteFilePath,
                DestinationPath = localDir,
            };

            var vm = new TransferItemViewModel(model);
            Transfers.Insert(0, vm);

            var connection = _activeConnection;
            _transferQueue.Enqueue(vm, (ct, progress) =>
                SFTPService.DownloadFileWithNewClientAsync(connection, remoteFilePath, localDir, progress, ct));
        }

        StatusText = $"Queued {expanded.Count} download(s)";
    }

    public Task DownloadItemsAsync(IEnumerable<FileItemViewModel> selectedItems, string localDirectory, CancellationToken cancellationToken = default)
        => DownloadItemsAsync(selectedItems, localDirectory, SyncConflictMode.Skip, cancellationToken);

    public async Task SyncCurrentFolderToLocalAsync(string localDirectory, SyncConflictMode conflictMode, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return;

        if (_activeConnection is null)
            return;

        if (string.IsNullOrWhiteSpace(localDirectory))
            return;

        Directory.CreateDirectory(localDirectory);

        await _backgroundSync.QueueRecursiveSyncAsync(
            _activeConnection,
            remoteRoot: CurrentPath,
            localRoot: localDirectory,
            conflictMode: conflictMode,
            onNewTransfer: vm => Transfers.Insert(0, vm),
            cancellationToken: cancellationToken);

        StatusText = "Queued recursive sync";
    }

    public Task SyncCurrentFolderToLocalAsync(string localDirectory, CancellationToken cancellationToken = default)
        => SyncCurrentFolderToLocalAsync(localDirectory, SyncConflictMode.Skip, cancellationToken);

    public async Task DeleteItemsAsync(IEnumerable<FileItemViewModel> selectedItems, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return;

        if (selectedItems is null)
            return;

        var items = selectedItems.ToArray();
        if (items.Length == 0)
            return;

        StatusText = $"Deleting {items.Length} item(s)...";

        foreach (var item in items)
        {
            await _sftpService.DeletePathAsync(item.FullPath, item.IsDirectory, cancellationToken);
        }

        await RefreshAsync(cancellationToken);
        StatusText = "Delete complete";
    }

    public async Task CreateFolderAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return;

        if (string.IsNullOrWhiteSpace(name))
            return;

        var remote = CurrentPath == "/" ? "/" + name : CurrentPath.TrimEnd('/') + "/" + name;
        await _sftpService.CreateDirectoryAsync(remote, cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    public async Task RenameItemAsync(FileItemViewModel item, string newName, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return;

        if (item is null)
            return;

        if (string.IsNullOrWhiteSpace(newName) || string.Equals(newName, item.Name, StringComparison.Ordinal))
            return;

        await _sftpService.RenameAsync(item.FullPath, newName, cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    public async Task<string?> DownloadToTempAsync(FileItemViewModel item, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return null;

        if (_activeConnection is null)
            return null;

        if (item is null || item.IsDirectory)
            return null;

        var tempRoot = Path.Combine(Path.GetTempPath(), "SFTP-Browser", "OpenCache");
        Directory.CreateDirectory(tempRoot);

        var localPath = Path.Combine(tempRoot, item.Name);

        // Reuse existing download primitive.
        await SFTPService.DownloadFileWithNewClientAsync(
            _activeConnection,
            item.FullPath,
            tempRoot,
            progress: null,
            cancellationToken);

        return File.Exists(localPath) ? localPath : null;
    }

    public async Task<bool> HasSyncConflictsAsync(string localDirectory, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return false;

        if (_activeConnection is null)
            return false;

        if (string.IsNullOrWhiteSpace(localDirectory))
            return false;

        Directory.CreateDirectory(localDirectory);

        try
        {
            return await _backgroundSync.HasConflictsAsync(
                _activeConnection,
                remoteRoot: CurrentPath,
                localRoot: localDirectory,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // If the scan fails, fall back to showing the dialog (conservative).
            return true;
        }
    }

    public async Task BiDirectionalSyncCurrentFolderAsync(string localDirectory, SyncConflictMode conflictMode, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return;

        if (_activeConnection is null)
            return;

        if (string.IsNullOrWhiteSpace(localDirectory))
            return;

        Directory.CreateDirectory(localDirectory);

        await _backgroundSync.QueueBiDirectionalSyncAsync(
            _activeConnection,
            remoteRoot: CurrentPath,
            localRoot: localDirectory,
            conflictMode: conflictMode,
            onNewTransfer: vm => Transfers.Insert(0, vm),
            cancellationToken: cancellationToken);

        StatusText = "Queued bi-directional sync";
    }
}
