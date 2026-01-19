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
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
        => await ViewModel.SaveAsync();

    private async void Reload_Click(object sender, RoutedEventArgs e)
        => await ViewModel.LoadAsync();
}
