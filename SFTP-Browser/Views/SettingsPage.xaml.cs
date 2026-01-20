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
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsCheckingForUpdates))
        {
            CheckUpdatesButton.IsEnabled = !ViewModel.IsCheckingForUpdates;
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
        => await ViewModel.SaveAsync();

    private async void Reload_Click(object sender, RoutedEventArgs e)
        => await ViewModel.LoadAsync();
}
