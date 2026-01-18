using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace WinUI_SFTP_Browser;

public partial class FileItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _fullPath;

    [ObservableProperty]
    private string _type;

    [ObservableProperty]
    private string _size;

    [ObservableProperty]
    private string _dateModified;

    [ObservableProperty]
    private string _icon;

    [ObservableProperty]
    private bool _isDirectory;

    [ObservableProperty]
    private long _sizeInBytes;

    [ObservableProperty]
    private DateTime _modifiedDate;

    public FileItemViewModel(string name, string fullPath, bool isDirectory, long sizeInBytes, DateTime modifiedDate)
    {
        _name = name;
        _fullPath = fullPath;
        _isDirectory = isDirectory;
        _sizeInBytes = sizeInBytes;
        _modifiedDate = modifiedDate;

        // Set icon based on type
        if (isDirectory)
        {
            _icon = "\uE8B7"; // Folder icon
            _type = "File folder";
            _size = "";
        }
        else
        {
            _icon = GetFileIcon(name);
            _type = GetFileType(name);
            _size = FormatFileSize(sizeInBytes);
        }

        _dateModified = modifiedDate.ToString("M/d/yyyy h:mm tt");
    }

    private static string GetFileIcon(string fileName)
    {
        var extension = System.IO.Path.GetExtension(fileName)?.ToLowerInvariant();
        return extension switch
        {
            ".txt" => "\uE8A5", // Document
            ".pdf" => "\uE8A5",
            ".doc" or ".docx" => "\uE8A5",
            ".xls" or ".xlsx" => "\uE8A5",
            ".ppt" or ".pptx" => "\uE8A5",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "\uEB9F", // Picture
            ".mp4" or ".avi" or ".mkv" or ".mov" => "\uE8B2", // Video
            ".mp3" or ".wav" or ".flac" => "\uE8D6", // Music
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "\uE8B5", // Zip
            ".exe" or ".msi" => "\uE756", // Application
            ".cs" or ".cpp" or ".h" or ".java" or ".py" or ".js" or ".ts" => "\uE943", // Code
            _ => "\uE8A5" // Generic document
        };
    }

    private static string GetFileType(string fileName)
    {
        var extension = System.IO.Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
            return "File";

        return extension switch
        {
            ".txt" => "Text Document",
            ".pdf" => "PDF Document",
            ".doc" or ".docx" => "Word Document",
            ".xls" or ".xlsx" => "Excel Document",
            ".ppt" or ".pptx" => "PowerPoint Presentation",
            ".jpg" or ".jpeg" => "JPEG Image",
            ".png" => "PNG Image",
            ".gif" => "GIF Image",
            ".bmp" => "Bitmap Image",
            ".mp4" => "MP4 Video",
            ".avi" => "AVI Video",
            ".mkv" => "MKV Video",
            ".mov" => "MOV Video",
            ".mp3" => "MP3 Audio",
            ".wav" => "WAV Audio",
            ".flac" => "FLAC Audio",
            ".zip" => "ZIP Archive",
            ".rar" => "RAR Archive",
            ".7z" => "7-Zip Archive",
            ".tar" => "TAR Archive",
            ".gz" => "GZIP Archive",
            ".exe" => "Application",
            ".msi" => "Windows Installer",
            ".cs" => "C# Source File",
            ".cpp" => "C++ Source File",
            ".h" => "C/C++ Header File",
            ".java" => "Java Source File",
            ".py" => "Python Script",
            ".js" => "JavaScript File",
            ".ts" => "TypeScript File",
            _ => $"{extension.TrimStart('.')} File".ToUpperInvariant()
        };
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 bytes";
        
        string[] sizes = { "bytes", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.##} {sizes[order]}";
    }
}
