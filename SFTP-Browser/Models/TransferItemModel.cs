using System;

namespace SFTP_Browser.Models;

public sealed class TransferItemModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public TransferDirection Direction { get; set; }

    public string SourcePath { get; set; } = "";

    public string DestinationPath { get; set; } = "";

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
