using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace WinUI_SFTP_Browser;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SftpService _sftpService;
    private readonly Stack<string> _backHistory;
    private readonly Stack<string> _forwardHistory;

    [ObservableProperty]
    private ObservableCollection<FileItemViewModel> _items;

    [ObservableProperty]
    private string _currentPath;

    [ObservableProperty]
    private string _statusText;

    [ObservableProperty]
    private string _connectionStatus;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoForward;

    [ObservableProperty]
    private bool _showEmptyState;

    public MainWindowViewModel()
    {
        _sftpService = new SftpService();
        _backHistory = new Stack<string>();
        _forwardHistory = new Stack<string>();
        _items = new ObservableCollection<FileItemViewModel>();
        _currentPath = "";
        _statusText = "Ready";
        _connectionStatus = "Not connected";
        _isConnected = false;
        _isLoading = false;
        _hasSelection = false;
        _canGoBack = false;
        _canGoForward = false;
        _showEmptyState = true;
    }

    public async Task ConnectAsync(SftpConnectionInfo connectionInfo)
    {
        try
        {
            IsLoading = true;
            StatusText = "Connecting...";
            
            await _sftpService.ConnectAsync(connectionInfo);
            
            IsConnected = true;
            ConnectionStatus = $"Connected to {connectionInfo.Host}";
            CurrentPath = "/";
            
            await LoadDirectoryAsync("/");
        }
        catch (Exception ex)
        {
            StatusText = $"Connection failed: {ex.Message}";
            IsConnected = false;
            ConnectionStatus = "Not connected";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshAsync()
    {
        if (!IsConnected) return;
        await LoadDirectoryAsync(CurrentPath);
    }

    public async Task NavigateToAsync(string path)
    {
        if (!IsConnected) return;
        
        _backHistory.Push(CurrentPath);
        _forwardHistory.Clear();
        CanGoBack = true;
        CanGoForward = false;
        
        await LoadDirectoryAsync(path);
    }

    public async void NavigateBack()
    {
        if (_backHistory.Count == 0) return;
        
        _forwardHistory.Push(CurrentPath);
        var path = _backHistory.Pop();
        CanGoBack = _backHistory.Count > 0;
        CanGoForward = true;
        
        await LoadDirectoryAsync(path);
    }

    public async void NavigateForward()
    {
        if (_forwardHistory.Count == 0) return;
        
        _backHistory.Push(CurrentPath);
        var path = _forwardHistory.Pop();
        CanGoBack = true;
        CanGoForward = _forwardHistory.Count > 0;
        
        await LoadDirectoryAsync(path);
    }

    private async Task LoadDirectoryAsync(string path)
    {
        try
        {
            IsLoading = true;
            ShowEmptyState = false;
            StatusText = $"Loading {path}...";
            
            var files = await _sftpService.ListDirectoryAsync(path);
            
            Items.Clear();
            foreach (var file in files.OrderBy(f => !f.IsDirectory).ThenBy(f => f.Name))
            {
                Items.Add(file);
            }
            
            CurrentPath = path;
            StatusText = $"{files.Count} item(s)";
            ShowEmptyState = !IsConnected;
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading directory: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task UploadFilesAsync(IReadOnlyList<StorageFile> files)
    {
        try
        {
            IsLoading = true;
            StatusText = $"Uploading {files.Count} file(s)...";
            
            await _sftpService.UploadFilesAsync(files, CurrentPath);
            
            await RefreshAsync();
            StatusText = $"Uploaded {files.Count} file(s)";
        }
        catch (Exception ex)
        {
            StatusText = $"Upload failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task DownloadItemsAsync(List<FileItemViewModel> items, StorageFolder folder)
    {
        try
        {
            IsLoading = true;
            StatusText = $"Downloading {items.Count} item(s)...";
            
            await _sftpService.DownloadItemsAsync(items, folder);
            
            StatusText = $"Downloaded {items.Count} item(s)";
        }
        catch (Exception ex)
        {
            StatusText = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task DeleteItemsAsync(List<FileItemViewModel> items)
    {
        try
        {
            IsLoading = true;
            StatusText = $"Deleting {items.Count} item(s)...";
            
            await _sftpService.DeleteItemsAsync(items);
            
            await RefreshAsync();
            StatusText = $"Deleted {items.Count} item(s)";
        }
        catch (Exception ex)
        {
            StatusText = $"Delete failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task CreateFolderAsync(string folderName)
    {
        try
        {
            IsLoading = true;
            StatusText = $"Creating folder {folderName}...";
            
            await _sftpService.CreateDirectoryAsync(CurrentPath, folderName);
            
            await RefreshAsync();
            StatusText = $"Created folder {folderName}";
        }
        catch (Exception ex)
        {
            StatusText = $"Create folder failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RenameItemAsync(FileItemViewModel item, string newName)
    {
        try
        {
            IsLoading = true;
            StatusText = $"Renaming {item.Name}...";
            
            await _sftpService.RenameItemAsync(item.FullPath, CurrentPath + "/" + newName);
            
            await RefreshAsync();
            StatusText = $"Renamed {item.Name} to {newName}";
        }
        catch (Exception ex)
        {
            StatusText = $"Rename failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SyncFolderAsync(StorageFolder localFolder)
    {
        try
        {
            IsLoading = true;
            StatusText = "Syncing folder...";
            
            await _sftpService.SyncFolderAsync(CurrentPath, localFolder);
            
            await RefreshAsync();
            StatusText = "Sync completed";
        }
        catch (Exception ex)
        {
            StatusText = $"Sync failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void UpdateSelection(List<FileItemViewModel> selectedItems)
    {
        HasSelection = selectedItems.Count > 0;
    }
}
