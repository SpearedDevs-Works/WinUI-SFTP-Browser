using System;

namespace SFTP_Browser.Models;

public sealed class SftpItemModel
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required bool IsDirectory { get; init; }

    public long Length { get; init; }
    public DateTimeOffset? LastWriteTime { get; init; }
}
