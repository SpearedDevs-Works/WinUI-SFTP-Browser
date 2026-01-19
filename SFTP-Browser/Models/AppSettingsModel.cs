using System;
using System.Collections.Generic;

namespace SFTP_Browser.Models;

public sealed class AppSettingsModel
{
    public List<SftpBookmarkModel> Bookmarks { get; set; } = new();

    public List<SftpRecentConnectionModel> RecentConnections { get; set; } = new();

    public int MaxRecentConnections { get; set; } = 10;

    public BackgroundSyncSettingsModel BackgroundSync { get; set; } = new();

    public bool NotificationsEnabled { get; set; } = true;
}
