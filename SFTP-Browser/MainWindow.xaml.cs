using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using WinRT.Interop;

using SFTP_Browser.Views;

namespace SFTP_Browser
{
    public sealed partial class MainWindow : Window
    {
        private AppWindow m_AppWindow;

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        private static Windows.Graphics.PointInt32 GetCenteredPosition(AppWindow appWindow, int width, int height)
        {
            DisplayArea displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
            Windows.Graphics.RectInt32 workArea = displayArea.WorkArea;

            int x = workArea.X + (workArea.Width - width) / 2;
            int y = workArea.Y + (workArea.Height - height) / 2;

            x = Math.Max(workArea.X, x);
            y = Math.Max(workArea.Y, y);

            return new Windows.Graphics.PointInt32(x, y);
        }

        public MainWindow()
        {
            InitializeComponent();

            m_AppWindow = GetAppWindowForCurrentWindow();
            AppWindowTitleBar titleBar = m_AppWindow.TitleBar;

            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.PreferredMinimumHeight = 600;
            presenter.PreferredMinimumWidth = 800;
            presenter.IsResizable = true;
            m_AppWindow.SetPresenter(presenter);

            int windowWidth = 1280;
            int windowHeight = 720;

            m_AppWindow.Resize(new Windows.Graphics.SizeInt32(windowWidth, windowHeight));
            m_AppWindow.Move(GetCenteredPosition(m_AppWindow, windowWidth, windowHeight));

            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            SetTitleBar(AppTitleBar);
        }

        private TabViewItem NewConnectionTabItem(string title)
        {
            var tabViewItem = new TabViewItem
            {
                Header = title,
                IconSource = new SymbolIconSource { Symbol = Symbol.World }
            };

            var content = new ConnectionTabContent();
            content.TabTitleChanged += newTitle =>
            {
                _ = DispatcherQueue.TryEnqueue(() => tabViewItem.Header = newTitle);
            };

            tabViewItem.Content = content;
            return tabViewItem;
        }

        private void ShellTabView_Loaded(object sender, RoutedEventArgs e)
        {
            if (ShellTabView.TabItems.Count == 0)
            {
                var initialTabViewItem = NewConnectionTabItem("New SFTP Connection #1");
                ShellTabView.TabItems.Add(initialTabViewItem);
                ShellTabView.SelectedItem = initialTabViewItem;
            }
        }

        private async void ShellTabView_TabCloseRequestedAsync(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            ShellTabView.TabItems.Remove(args.Tab);

            if (ShellTabView.TabItems.Count == 0)
            {
                var exitDialog = new ContentDialog
                {
                    Title = "Exit Application",
                    Content = "There are no more tabs open. Do you want to exit the application?",
                    CloseButtonText = "Cancel",
                    PrimaryButtonText = "Exit",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = Content.XamlRoot
                };

                var result = await exitDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                    Application.Current.Exit();
                else
                {
                    var newTab = NewConnectionTabItem("New SFTP Connection #1");
                    ShellTabView.TabItems.Add(newTab);
                    ShellTabView.SelectedItem = newTab;
                }
            }
        }

        private void ShellTabView_AddTabButtonClick(TabView sender, object args)
        {
            int currentTabNumber = ShellTabView.TabItems.Count + 1;
            var newTab = NewConnectionTabItem($"New SFTP Connection #{currentTabNumber}");
            ShellTabView.TabItems.Add(newTab);
            ShellTabView.SelectedItem = newTab;
        }

        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Settings",
                CloseButtonText = "Close",
                XamlRoot = Content.XamlRoot,
                Content = new Frame
                {
                    Content = new Views.SettingsPage()
                }
            };

            await dialog.ShowAsync();
        }
    }
}