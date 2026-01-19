using System;

namespace SFTP_Browser.Models;

public sealed class BackgroundSyncSettingsModel
{
    public bool Enabled { get; set; }

    /// <summary>
    /// Interval between sync runs.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Local folder used for scheduled sync.
    /// </summary>
    public string LocalFolder { get; set; } = "";

    /// <summary>
    /// Remote folder path to sync.
    /// </summary>
    public string RemoteFolder { get; set; } = "/";

    public SyncConflictMode ConflictMode { get; set; } = SyncConflictMode.Skip;

    /// <summary>
    /// Last run time (UTC).
    /// </summary>
    public DateTimeOffset? LastRunUtc { get; set; }
}
