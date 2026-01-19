using System;
using CommunityToolkit.Mvvm.ComponentModel;
using SFTP_Browser.Models;

namespace SFTP_Browser.ViewModels;

public sealed partial class FileItemViewModel : ObservableObject
{
    public FileItemViewModel(SftpItemModel model)
    {
        Name = model.Name;
        FullPath = model.FullPath;
        IsDirectory = model.IsDirectory;
        SizeBytes = model.Length;
        LastWriteTime = model.LastWriteTime;
    }

    public string Name { get; }
    public string FullPath { get; }
    public bool IsDirectory { get; }

    public long SizeBytes { get; }
    public DateTimeOffset? LastWriteTime { get; }

    public string DateModified => LastWriteTime?.LocalDateTime.ToString("g") ?? "";
    public string Type => IsDirectory ? "Folder" : "File";
    public string Size => IsDirectory ? "" : FormatSize(SizeBytes);

    public string Icon => IsDirectory ? "\uE8B7" : "\uE8A5"; // folder, document

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }
}
