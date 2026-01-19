#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using SFTP_Browser.Models;
using SFTP_Browser.Services;
using SFTP_Browser.ViewModels;

namespace SFTP_Browser.Views;

public sealed partial class ConnectionTabContent : UserControl
{
    public ConnectionTabViewModel ViewModel { get; }

    public event Action<string>? TabTitleChanged;

    private readonly SettingsService _settingsService = new();

    public ConnectionTabContent()
    {
        InitializeComponent();

        ViewModel = new ConnectionTabViewModel(DispatcherQueue);
        DataContext = ViewModel;

        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ConnectionTabViewModel.TabTitle))
                TabTitleChanged?.Invoke(ViewModel.TabTitle);
        };
    }

    private static Grid CreateConnectFormGrid()
    {
        var grid = new Grid
        {
            ColumnSpacing = 12,
            RowSpacing = 8,
            MinWidth = 420,
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        return grid;
    }

    private static void AddLabeledRow(Grid grid, int row, string label, FrameworkElement element)
    {
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var labelBlock = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetRow(labelBlock, row);
        Grid.SetColumn(labelBlock, 0);

        Grid.SetRow(element, row);
        Grid.SetColumn(element, 1);

        grid.Children.Add(labelBlock);
        grid.Children.Add(element);
    }

    private async void NewConnection_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Connect",
            PrimaryButtonText = "Connect",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        var host = new TextBox { PlaceholderText = "example.com" };
        var port = new NumberBox { Value = 22, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, Width = 140 };
        var user = new TextBox { PlaceholderText = "Username" };

        var authMode = new ComboBox
        {
            ItemsSource = new[] { "Password", "Private key" },
            SelectedIndex = 0
        };

        var password = new PasswordBox();

        var keyPath = new TextBox { PlaceholderText = "Private key file path", IsReadOnly = true };
        var browseKey = new Button { Content = "Browse..." };
        var keyPassphrase = new PasswordBox();

        var keyRow = new Grid();
        keyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        keyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        keyRow.Children.Add(keyPath);
        Grid.SetColumn(browseKey, 1);
        browseKey.Margin = new Thickness(8, 0, 0, 0);
        keyRow.Children.Add(browseKey);

        var form = CreateConnectFormGrid();
        AddLabeledRow(form, 0, "Host", host);

        var portHostRow = new Grid { ColumnSpacing = 8 };
        portHostRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        portHostRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        portHostRow.Children.Add(port);
        Grid.SetColumn(user, 1);
        portHostRow.Children.Add(user);
        AddLabeledRow(form, 1, "Port / User", portHostRow);

        AddLabeledRow(form, 2, "Authentication", authMode);

        var passwordPanel = new Grid();
        passwordPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        passwordPanel.Children.Add(password);

        var keyPanel = new StackPanel { Spacing = 8, Children = { keyRow, keyPassphrase } };

        AddLabeledRow(form, 3, "Password", passwordPanel);
        AddLabeledRow(form, 4, "Key / Passphrase", keyPanel);

        void UpdateAuthUi()
        {
            bool isPassword = authMode.SelectedIndex == 0;
            passwordPanel.Visibility = isPassword ? Visibility.Visible : Visibility.Collapsed;
            keyPanel.Visibility = isPassword ? Visibility.Collapsed : Visibility.Visible;
        }

        authMode.SelectionChanged += (_, _) => UpdateAuthUi();

        browseKey.Click += async (_, _) =>
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.FileTypeFilter.Add("*");

                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

                var file = await picker.PickSingleFileAsync();
                if (file is not null)
                    keyPath.Text = file.Path;
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to pick file: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot,
                };
                await errorDialog.ShowAsync();
            }
        };

        UpdateAuthUi();
        dialog.Content = form;

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        // Split combined host/user row back out
        var portValue = (int)port.Value;
        var username = user.Text;

        if (authMode.SelectedIndex == 0)
        {
            await ViewModel.ConnectWithPasswordAsync((host.Text, portValue, username, password.Password));
            return;
        }

        await ViewModel.ConnectWithPrivateKeyAsync((host.Text, portValue, username, keyPath.Text, string.IsNullOrEmpty(keyPassphrase.Password) ? null : keyPassphrase.Password));
    }

    private async Task<bool> PromptAndConnectAsync(string host, int port, string username)
    {
        var dialog = new ContentDialog
        {
            Title = "Connect",
            PrimaryButtonText = "Connect",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        var authMode = new ComboBox
        {
            ItemsSource = new[] { "Password", "Private key" },
            SelectedIndex = 0
        };

        var password = new PasswordBox();

        var keyPath = new TextBox { PlaceholderText = "Private key file path", IsReadOnly = true };
        var browseKey = new Button { Content = "Browse..." };
        var keyPassphrase = new PasswordBox();

        var keyRow = new Grid();
        keyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        keyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        keyRow.Children.Add(keyPath);
        Grid.SetColumn(browseKey, 1);
        browseKey.Margin = new Thickness(8, 0, 0, 0);
        keyRow.Children.Add(browseKey);

        var form = CreateConnectFormGrid();
        AddLabeledRow(form, 0, "Server", new TextBlock { Text = $"{username}@{host}:{port}" });
        AddLabeledRow(form, 1, "Authentication", authMode);
        AddLabeledRow(form, 2, "Password", password);
        AddLabeledRow(form, 3, "Key", keyRow);
        AddLabeledRow(form, 4, "Passphrase", keyPassphrase);

        void UpdateAuthUi()
        {
            bool isPassword = authMode.SelectedIndex == 0;
            password.Visibility = isPassword ? Visibility.Visible : Visibility.Collapsed;
            keyRow.Visibility = isPassword ? Visibility.Collapsed : Visibility.Visible;
            keyPassphrase.Visibility = isPassword ? Visibility.Collapsed : Visibility.Visible;
        }

        authMode.SelectionChanged += (_, _) => UpdateAuthUi();

        browseKey.Click += async (_, _) =>
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.FileTypeFilter.Add("*");

                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

                var file = await picker.PickSingleFileAsync();
                if (file is not null)
                    keyPath.Text = file.Path;
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to pick file: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot,
                };
                await errorDialog.ShowAsync();
            }
        };

        UpdateAuthUi();
        dialog.Content = form;

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return false;

        if (authMode.SelectedIndex == 0)
        {
            await ViewModel.ConnectWithPasswordAsync((host, port, username, password.Password));
            return true;
        }

        await ViewModel.ConnectWithPrivateKeyAsync((host, port, username, keyPath.Text, string.IsNullOrEmpty(keyPassphrase.Password) ? null : keyPassphrase.Password));
        return true;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        // placeholder
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
        => await ViewModel.RefreshAsync();

    private async void Upload_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsConnected)
            return;

        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.FileTypeFilter.Add("*");

        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

        var files = await picker.PickMultipleFilesAsync();
        if (files is null || files.Count == 0)
            return;

        var paths = files.Select(f => f.Path).Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        await ViewModel.UploadFilesAsync(paths);
    }

    private static async Task<SyncConflictMode?> PromptConflictModeAsync(XamlRoot xamlRoot, string title)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            PrimaryButtonText = "Overwrite",
            SecondaryButtonText = "Skip",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = xamlRoot,
        };

        var result = await dialog.ShowAsync();
        return result switch
        {
            ContentDialogResult.Primary => SyncConflictMode.Overwrite,
            ContentDialogResult.Secondary => SyncConflictMode.Skip,
            _ => null
        };
    }

    private async void Download_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsConnected)
            return;

        if (FileListView.SelectedItems is null || FileListView.SelectedItems.Count == 0)
            return;

        var conflict = await PromptConflictModeAsync(XamlRoot, "Download conflicts");
        if (conflict is null)
            return;

        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.FileTypeFilter.Add("*");

        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is null)
            return;

        var localDir = folder.Path;

        var selected = FileListView.SelectedItems
            .OfType<SFTP_Browser.ViewModels.FileItemViewModel>()
            .ToArray();

        await ViewModel.DownloadItemsAsync(selected, localDir, conflict.Value);
    }

    private async void Sync_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsConnected)
            return;

        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.FileTypeFilter.Add("*");

        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is null)
            return;

        var localRoot = folder.Path;

        var hasConflicts = await ViewModel.HasSyncConflictsAsync(localRoot);
        var conflictMode = SyncConflictMode.Skip;

        if (hasConflicts)
        {
            var picked = await PromptConflictModeAsync(XamlRoot, "Sync conflicts");
            if (picked is null)
                return;

            conflictMode = picked.Value;
        }

        await ViewModel.BiDirectionalSyncCurrentFolderAsync(localRoot, conflictMode);
    }

    private async void Back_Click(object sender, RoutedEventArgs e)
        => await ViewModel.NavigateBackAsync();

    private async void Forward_Click(object sender, RoutedEventArgs e)
        => await ViewModel.NavigateForwardAsync();

    private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView lv)
            ViewModel.UpdateSelectionCount(lv.SelectedItems?.Count ?? 0);
    }

    private async void FileListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (FileListView.SelectedItem is not SFTP_Browser.ViewModels.FileItemViewModel item)
            return;

        if (item.IsDirectory)
        {
            await ViewModel.NavigateToAsync(item.FullPath);
            return;
        }

        if (!ViewModel.IsConnected)
            return;

        var localPath = await ViewModel.DownloadToTempAsync(item);
        if (string.IsNullOrWhiteSpace(localPath))
            return;

        var action = new FileActionService();
        _ = await action.OpenWithDefaultAppAsync(localPath);
    }

    private void FileListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (Resources["FileContextMenu"] is MenuFlyout flyout)
            flyout.ShowAt((FrameworkElement)sender, e.GetPosition((FrameworkElement)sender));
    }

    private async void OpenContextMenu_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsConnected)
            return;

        if (FileListView.SelectedItem is not SFTP_Browser.ViewModels.FileItemViewModel item)
            return;

        if (item.IsDirectory)
            return;

        var action = new FileActionService();
        var localPath = await ViewModel.DownloadToTempAsync(item);
        if (string.IsNullOrWhiteSpace(localPath))
            return;

        _ = await action.OpenWithDefaultAppAsync(localPath);
    }

    private async void DeleteContextMenu_Click(object sender, RoutedEventArgs e)
        => Delete_Click(sender, e);

    private async void RenameContextMenu_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsConnected)
            return;

        if (FileListView.SelectedItem is not SFTP_Browser.ViewModels.FileItemViewModel item)
            return;

        var dialog = new ContentDialog
        {
            Title = "Rename",
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        var nameBox = new TextBox { Text = item.Name };
        dialog.Content = nameBox;

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        // Delegate to service through the view model by calling refresh after rename.
        await ViewModel.RenameItemAsync(item, nameBox.Text);
    }

    private async void PropertiesContextMenu_Click(object sender, RoutedEventArgs e)
    {
        if (FileListView.SelectedItem is not SFTP_Browser.ViewModels.FileItemViewModel item)
            return;

        var dialog = new ContentDialog
        {
            Title = "Properties",
            CloseButtonText = "Close",
            XamlRoot = XamlRoot,
            Content = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock { Text = $"Name: {item.Name}" },
                    new TextBlock { Text = $"Type: {item.Type}" },
                    new TextBlock { Text = $"Path: {item.FullPath}" },
                    new TextBlock { Text = $"Size: {item.Size}" },
                    new TextBlock { Text = $"Modified: {item.DateModified}" },
                }
            }
        };

        await dialog.ShowAsync();
    }

    private async void Bookmarks_Click(object sender, RoutedEventArgs e)
    {
        var settings = await _settingsService.LoadAsync();

        var recents = new ListView
        {
            SelectionMode = ListViewSelectionMode.Single,
            ItemsSource = settings.RecentConnections,
        };

        var bookmarks = new ListView
        {
            SelectionMode = ListViewSelectionMode.Single,
            ItemsSource = settings.Bookmarks,
        };

        var tabs = new TabView
        {
            IsAddTabButtonVisible = false,
            TabItems =
            {
                new TabViewItem { Header = "Recents", Content = recents },
                new TabViewItem { Header = "Bookmarks", Content = bookmarks }
            }
        };

        var dialog = new ContentDialog
        {
            Title = "Bookmarks & Recents",
            PrimaryButtonText = "Connect",
            SecondaryButtonText = "Save Bookmark",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Primary,
            Content = tabs,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (tabs.SelectedIndex == 0 && recents.SelectedItem is SftpRecentConnectionModel rc)
            {
                await PromptAndConnectAsync(rc.Host, rc.Port, rc.Username);
            }
            else if (tabs.SelectedIndex == 1 && bookmarks.SelectedItem is SftpBookmarkModel bm)
            {
                await PromptAndConnectAsync(bm.Host, bm.Port, bm.Username);
            }
        }
        else if (result == ContentDialogResult.Secondary)
        {
            if (!ViewModel.IsConnected)
                return;

            var saveDialog = new ContentDialog
            {
                Title = "Bookmark name",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            var nameBox = new TextBox { PlaceholderText = "e.g. My Server" };
            saveDialog.Content = nameBox;

            var saveResult = await saveDialog.ShowAsync();
            if (saveResult != ContentDialogResult.Primary)
                return;

            // Best-effort: parse current title (user@host:port)
            var title = ViewModel.TabTitle;
            var at = title.IndexOf('@');
            var colon = title.LastIndexOf(':');
            if (at <= 0 || colon <= at + 1)
                return;

            var username = title[..at];
            var host = title[(at + 1)..colon];
            if (!int.TryParse(title[(colon + 1)..], out var port))
                port = 22;

            settings.Bookmarks.RemoveAll(b => b.Username == username && b.Host == host && b.Port == port);
            settings.Bookmarks.Add(new SftpBookmarkModel { Name = nameBox.Text, Host = host, Port = port, Username = username });
            await _settingsService.SaveAsync(settings);
        }
    }

    private async void Disconnect_Click(object sender, RoutedEventArgs e)
        => await ViewModel.DisconnectAsync();

    private void FileListView_DragOver(object sender, DragEventArgs e)
    {
        if (!ViewModel.IsConnected)
        {
            e.AcceptedOperation = DataPackageOperation.None;
            return;
        }

        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Upload to SFTP";
            e.DragUIOverride.IsCaptionVisible = true;
        }
        else
        {
            e.AcceptedOperation = DataPackageOperation.None;
        }

        e.Handled = true;
    }

    private async void FileListView_Drop(object sender, DragEventArgs e)
    {
        if (!ViewModel.IsConnected)
            return;

        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            return;

        var items = await e.DataView.GetStorageItemsAsync();
        var files = items.OfType<Windows.Storage.StorageFile>().ToArray();

        var paths = files.Select(f => f.Path).Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        await ViewModel.UploadFilesAsync(paths);

        e.Handled = true;
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsConnected)
            return;

        var selected = FileListView.SelectedItems
            .OfType<SFTP_Browser.ViewModels.FileItemViewModel>()
            .ToArray();

        if (selected.Length == 0)
            return;

        var dialog = new ContentDialog
        {
            Title = "Delete items?",
            Content = $"Are you sure you want to delete {selected.Length} item(s)?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        await ViewModel.DeleteItemsAsync(selected);
    }

    private async void NewFolder_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsConnected)
            return;

        var dialog = new ContentDialog
        {
            Title = "New Folder",
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        var nameBox = new TextBox { PlaceholderText = "Folder name" };
        dialog.Content = nameBox;

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        await ViewModel.CreateFolderAsync(nameBox.Text);
    }

    private async void DownloadContextMenu_Click(object sender, RoutedEventArgs e)
    {
        await Task.Yield();
        Download_Click(sender, e);
    }
}
