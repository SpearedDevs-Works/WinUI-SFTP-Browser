using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Windows.ApplicationModel;
using SFTP_Browser.Models;

#nullable enable

namespace SFTP_Browser.Services;

public sealed class NotificationService
{
    private readonly SettingsService _settings;

    public NotificationService(SettingsService settings)
    {
        _settings = settings;

        try
        {
            // Safe init. Registering is required for unpackaged scenarios too.
            AppNotificationManager.Default.Register();
        }
        catch
        {
            // ignore (will fallback)
        }
    }

    public async Task NotifyAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        var settings = await _settings.LoadAsync(cancellationToken);
        if (!settings.NotificationsEnabled)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        // Prefer Windows App SDK AppNotifications (works for packaged and can work for unpackaged with registration).
        try
        {
            var notification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
            return;
        }
        catch
        {
            // ignore and fallback
        }

        // Packaged fallback: toast XML via AppNotification.
        try
        {
            if (IsPackaged())
            {
                var xml = $"<toast><visual><binding template=\"ToastGeneric\"><text>{Escape(title)}</text><text>{Escape(message)}</text></binding></visual></toast>";
                AppNotificationManager.Default.Show(new AppNotification(xml));
                return;
            }
        }
        catch
        {
        }

        System.Diagnostics.Debug.WriteLine($"[Notification] {title}: {message}");
    }

    private static bool IsPackaged()
    {
        try
        {
            _ = Package.Current;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string Escape(string value)
        => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
