using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace WinUI_SFTP_Browser;

public class SftpService
{
    private SftpClient? _client;
    private bool _isConnected;

    public async Task ConnectAsync(SftpConnectionInfo connectionInfo)
    {
        await Task.Run(() =>
        {
            _client = new SftpClient(connectionInfo.Host, connectionInfo.Port, 
                                     connectionInfo.Username, connectionInfo.Password);
            _client.Connect();
            _isConnected = _client.IsConnected;
        });
    }

    public async Task<List<FileItemViewModel>> ListDirectoryAsync(string path)
    {
        if (_client == null || !_isConnected)
            throw new InvalidOperationException("Not connected to SFTP server");

        return await Task.Run(() =>
        {
            var items = new List<FileItemViewModel>();
            var files = _client.ListDirectory(path);

            foreach (var file in files)
            {
                // Skip . and ..
                if (file.Name == "." || file.Name == "..")
                    continue;

                items.Add(new FileItemViewModel(
                    file.Name,
                    file.FullName,
                    file.IsDirectory,
                    file.Length,
                    file.LastWriteTime
                ));
            }

            return items;
        });
    }

    public async Task UploadFilesAsync(IReadOnlyList<StorageFile> files, string remotePath)
    {
        if (_client == null || !_isConnected)
            throw new InvalidOperationException("Not connected to SFTP server");

        await Task.Run(async () =>
        {
            foreach (var file in files)
            {
                var remoteFilePath = $"{remotePath.TrimEnd('/')}/{file.Name}";
                
                using var stream = await file.OpenStreamForReadAsync();
                _client.UploadFile(stream, remoteFilePath);
            }
        });
    }

    public async Task DownloadItemsAsync(List<FileItemViewModel> items, StorageFolder localFolder)
    {
        if (_client == null || !_isConnected)
            throw new InvalidOperationException("Not connected to SFTP server");

        await Task.Run(async () =>
        {
            foreach (var item in items)
            {
                if (item.IsDirectory)
                {
                    await DownloadDirectoryAsync(item.FullPath, localFolder, item.Name);
                }
                else
                {
                    var localFile = await localFolder.CreateFileAsync(item.Name, 
                        CreationCollisionOption.GenerateUniqueName);
                    using var stream = await localFile.OpenStreamForWriteAsync();
                    _client.DownloadFile(item.FullPath, stream);
                }
            }
        });
    }

    private async Task DownloadDirectoryAsync(string remotePath, StorageFolder localParentFolder, string folderName)
    {
        var localFolder = await localParentFolder.CreateFolderAsync(folderName, 
            CreationCollisionOption.GenerateUniqueName);

        var files = _client.ListDirectory(remotePath);
        foreach (var file in files)
        {
            if (file.Name == "." || file.Name == "..")
                continue;

            if (file.IsDirectory)
            {
                await DownloadDirectoryAsync(file.FullName, localFolder, file.Name);
            }
            else
            {
                var localFile = await localFolder.CreateFileAsync(file.Name, 
                    CreationCollisionOption.GenerateUniqueName);
                using var stream = await localFile.OpenStreamForWriteAsync();
                _client.DownloadFile(file.FullName, stream);
            }
        }
    }

    public async Task DeleteItemsAsync(List<FileItemViewModel> items)
    {
        if (_client == null || !_isConnected)
            throw new InvalidOperationException("Not connected to SFTP server");

        await Task.Run(() =>
        {
            foreach (var item in items)
            {
                if (item.IsDirectory)
                {
                    DeleteDirectoryRecursive(item.FullPath);
                }
                else
                {
                    _client.DeleteFile(item.FullPath);
                }
            }
        });
    }

    private void DeleteDirectoryRecursive(string path)
    {
        var files = _client.ListDirectory(path);
        foreach (var file in files)
        {
            if (file.Name == "." || file.Name == "..")
                continue;

            if (file.IsDirectory)
            {
                DeleteDirectoryRecursive(file.FullName);
            }
            else
            {
                _client.DeleteFile(file.FullName);
            }
        }
        _client.DeleteDirectory(path);
    }

    public async Task CreateDirectoryAsync(string parentPath, string directoryName)
    {
        if (_client == null || !_isConnected)
            throw new InvalidOperationException("Not connected to SFTP server");

        await Task.Run(() =>
        {
            var fullPath = $"{parentPath.TrimEnd('/')}/{directoryName}";
            _client.CreateDirectory(fullPath);
        });
    }

    public async Task RenameItemAsync(string oldPath, string newPath)
    {
        if (_client == null || !_isConnected)
            throw new InvalidOperationException("Not connected to SFTP server");

        await Task.Run(() =>
        {
            _client.RenameFile(oldPath, newPath);
        });
    }

    public async Task SyncFolderAsync(string remotePath, StorageFolder localFolder)
    {
        if (_client == null || !_isConnected)
            throw new InvalidOperationException("Not connected to SFTP server");

        await Task.Run(async () =>
        {
            // Download all remote files to local folder
            var remoteFiles = _client.ListDirectory(remotePath);
            foreach (var file in remoteFiles)
            {
                if (file.Name == "." || file.Name == "..")
                    continue;

                if (file.IsDirectory)
                {
                    var subFolder = await localFolder.CreateFolderAsync(file.Name, 
                        CreationCollisionOption.OpenIfExists);
                    await DownloadDirectoryAsync(file.FullName, localFolder, file.Name);
                }
                else
                {
                    var localFile = await localFolder.CreateFileAsync(file.Name, 
                        CreationCollisionOption.ReplaceExisting);
                    using var stream = await localFile.OpenStreamForWriteAsync();
                    _client.DownloadFile(file.FullName, stream);
                }
            }
        });
    }

    public void Disconnect()
    {
        if (_client != null && _isConnected)
        {
            _client.Disconnect();
            _client.Dispose();
            _isConnected = false;
        }
    }
}
