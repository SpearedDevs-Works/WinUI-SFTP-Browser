using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

namespace WinUI_SFTP_Browser;

public sealed partial class MainWindow : Window
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();
        ViewModel = new MainWindowViewModel();
        
        // Set up title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        
        // Apply Mica background
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
    }

    private async void NewConnection_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ConnectionDialog
        {
            XamlRoot = this.Content.XamlRoot
        };
        
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.ConnectionInfo != null)
        {
            await ViewModel.ConnectAsync(dialog.ConnectionInfo);
        }
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        NewConnection_Click(sender, e);
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync();
    }

    private async void Upload_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add("*");
        
        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(picker, hwnd);
        
        var files = await picker.PickMultipleFilesAsync();
        if (files.Count > 0)
        {
            await ViewModel.UploadFilesAsync(files);
        }
    }

    private async void Download_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = FileListView.SelectedItems.Cast<FileItemViewModel>().ToList();
        if (selectedItems.Count == 0) return;

        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        
        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(picker, hwnd);
        
        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            await ViewModel.DownloadItemsAsync(selectedItems, folder);
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = FileListView.SelectedItems.Cast<FileItemViewModel>().ToList();
        if (selectedItems.Count == 0) return;

        var dialog = new ContentDialog
        {
            Title = "Delete items?",
            Content = $"Are you sure you want to delete {selectedItems.Count} item(s)?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteItemsAsync(selectedItems);
        }
    }

    private async void NewFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new TextInputDialog
        {
            Title = "New Folder",
            Message = "Enter folder name:",
            XamlRoot = this.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.InputText))
        {
            await ViewModel.CreateFolderAsync(dialog.InputText);
        }
    }

    private async void Sync_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        
        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(picker, hwnd);
        
        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            await ViewModel.SyncFolderAsync(folder);
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigateBack();
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigateForward();
    }

    private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.UpdateSelection(FileListView.SelectedItems.Cast<FileItemViewModel>().ToList());
    }

    private async void FileListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement element && element.DataContext is FileItemViewModel item)
        {
            if (item.IsDirectory)
            {
                await ViewModel.NavigateToAsync(item.FullPath);
            }
        }
    }

    private void FileListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement element && this.Content is FrameworkElement root)
        {
            var flyout = root.Resources["FileContextMenu"] as MenuFlyout;
            flyout?.ShowAt(element, e.GetPosition(element));
        }
    }

    private async void DownloadContextMenu_Click(object sender, RoutedEventArgs e)
    {
        Download_Click(sender, e);
    }

    private async void DeleteContextMenu_Click(object sender, RoutedEventArgs e)
    {
        Delete_Click(sender, e);
    }

    private async void RenameContextMenu_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = FileListView.SelectedItem as FileItemViewModel;
        if (selectedItem == null) return;

        var dialog = new TextInputDialog
        {
            Title = "Rename",
            Message = "Enter new name:",
            InputText = selectedItem.Name,
            XamlRoot = this.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.InputText))
        {
            await ViewModel.RenameItemAsync(selectedItem, dialog.InputText);
        }
    }

    private async void PropertiesContextMenu_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = FileListView.SelectedItem as FileItemViewModel;
        if (selectedItem == null) return;

        var dialog = new PropertiesDialog(selectedItem)
        {
            XamlRoot = this.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }
}
