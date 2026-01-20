using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SFTP_Browser.ViewModels;

#nullable enable

namespace SFTP_Browser.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = new();

    public SettingsPage()
    {
        this.InitializeComponent();
        _ = ViewModel.LoadAsync();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        this.Unloaded += SettingsPage_Unloaded;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsCheckingForUpdates))
        {
            CheckUpdatesButton.IsEnabled = !ViewModel.IsCheckingForUpdates;
        }
        else if (e.PropertyName == nameof(ViewModel.IsDownloadingUpdate))
        {
            DownloadUpdateButton.IsEnabled = !ViewModel.IsDownloadingUpdate;
            InstallUpdateButton.IsEnabled = !ViewModel.IsDownloadingUpdate;
            CheckUpdatesButton.IsEnabled = !ViewModel.IsDownloadingUpdate;
        }
    }

    private void SettingsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        // Cleanup old update files when page is unloaded
        ViewModel.CleanupOldUpdates();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
        => await ViewModel.SaveAsync();

    private async void Reload_Click(object sender, RoutedEventArgs e)
        => await ViewModel.LoadAsync();
}
