using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

#nullable enable

namespace SFTP_Browser.Services;

public sealed class FileActionService
{
    public async Task<bool> OpenWithDefaultAppAsync(string localFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(localFilePath))
            return false;

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var file = await StorageFile.GetFileFromPathAsync(localFilePath);
            return await Launcher.LaunchFileAsync(file);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ShowInExplorerAsync(string localFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(localFilePath))
            return false;

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var file = await StorageFile.GetFileFromPathAsync(localFilePath);
            var folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(localFilePath)!);
            return await Launcher.LaunchFolderAsync(folder, new FolderLauncherOptions
            {
                ItemsToSelect = { file }
            });
        }
        catch
        {
            return false;
        }
    }
}
