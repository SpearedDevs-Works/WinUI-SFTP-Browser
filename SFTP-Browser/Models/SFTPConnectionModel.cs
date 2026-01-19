using System;

#nullable enable

namespace SFTP_Browser.Models;

public sealed class SFTPConnectionModel
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "";

    public SftpAuthenticationMode AuthenticationMode { get; set; } = SftpAuthenticationMode.Password;

    public string? Password { get; set; }

    public string? PrivateKeyPath { get; set; }
    public string? PrivateKeyPassphrase { get; set; }

    public string InitialPath { get; set; } = "/";

    public override string ToString() => $"{Username}@{Host}:{Port}";
}
