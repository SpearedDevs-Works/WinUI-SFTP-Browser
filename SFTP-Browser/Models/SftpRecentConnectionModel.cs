using System;

namespace SFTP_Browser.Models;

public sealed class SftpRecentConnectionModel
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "";

    public DateTimeOffset LastUsedUtc { get; set; } = DateTimeOffset.UtcNow;

    public override string ToString() => $"{Username}@{Host}:{Port}";
}
