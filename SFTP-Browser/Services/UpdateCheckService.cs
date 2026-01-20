using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Octokit;

#nullable enable

namespace SFTP_Browser.Services;

public sealed class UpdateCheckService
{
    private readonly GitHubClient _gitHubClient;
    private readonly HttpClient _httpClient;
    private const string OwnerName = "SpearedDevs-Works";
    private const string RepositoryName = "WinUI-SFTP-Browser";

    public event EventHandler<UpdateProgressEventArgs>? DownloadProgressChanged;

    public UpdateCheckService()
    {
        _gitHubClient = new GitHubClient(new ProductHeaderValue("SFTP-Browser"));
        _httpClient = new HttpClient();
    }

    public async Task<Release?> CheckForUpdatesAsync(string currentVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            var releases = await _gitHubClient.Repository.Release.GetAll(OwnerName, RepositoryName, new ApiOptions { PageCount = 1, PageSize = 5 });

            if (releases.Count == 0)
                return null;

            foreach (var release in releases)
            {
                if (release.Prerelease)
                    continue;

                var latestVersion = release.TagName.TrimStart('v');
                if (IsNewerVersion(latestVersion, currentVersion))
                    return release;
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking for updates: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> DownloadReleaseAsync(Release release, CancellationToken cancellationToken = default)
    {
        try
        {
            if (release.Assets.Count == 0)
            {
                Debug.WriteLine("No assets found in release");
                return null;
            }

            var installerAsset = release.Assets.FirstOrDefault(a =>
                a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                a.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) ||
                a.Name.EndsWith(".msix", StringComparison.OrdinalIgnoreCase));

            if (installerAsset == null)
            {
                Debug.WriteLine("No installer asset found");
                return null;
            }

            var downloadPath = Path.Combine(Path.GetTempPath(), "SFTP-Browser-Updates");
            Directory.CreateDirectory(downloadPath);

            var filePath = Path.Combine(downloadPath, installerAsset.Name);

            using (var response = await _httpClient.GetAsync(installerAsset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes != -1L;

                using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                using (var fileStream = new FileStream(filePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var totalRead = 0L;
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) != 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        totalRead += bytesRead;

                        if (canReportProgress)
                        {
                            var progress = (int)((totalRead * 100) / totalBytes);
                            DownloadProgressChanged?.Invoke(this, new UpdateProgressEventArgs(progress));
                        }
                    }
                }
            }

            return filePath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error downloading update: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> InstallUpdateAsync(string installerPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(installerPath))
            {
                Debug.WriteLine("Installer file not found");
                return false;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true,
                Verb = "runas" // Request admin privileges if needed
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    Debug.WriteLine("Failed to start installer process");
                    return false;
                }

                // Wait for installer to complete with timeout
                var completed = process.WaitForExit(300000); // 5 minute timeout
                return completed && process.ExitCode == 0;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error installing update: {ex.Message}");
            return false;
        }
    }

    public void CleanupDownloads()
    {
        try
        {
            var downloadPath = Path.Combine(Path.GetTempPath(), "SFTP-Browser-Updates");
            if (Directory.Exists(downloadPath))
            {
                var files = Directory.GetFiles(downloadPath);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore errors during cleanup
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error cleaning up downloads: {ex.Message}");
        }
    }

    private static bool IsNewerVersion(string latestVersion, string currentVersion)
    {
        if (!Version.TryParse(latestVersion, out var latest) ||
            !Version.TryParse(currentVersion, out var current))
            return false;

        return latest > current;
    }
}

public sealed class UpdateProgressEventArgs : EventArgs
{
    public int ProgressPercentage { get; }

    public UpdateProgressEventArgs(int progressPercentage)
    {
        ProgressPercentage = progressPercentage;
    }
}
