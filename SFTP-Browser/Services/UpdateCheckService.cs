using System;
using System.Threading;
using System.Threading.Tasks;
using Octokit;

#nullable enable

namespace SFTP_Browser.Services;

public sealed class UpdateCheckService
{
    private readonly GitHubClient _gitHubClient;
    private const string OwnerName = "SpearedDevs-Works";
    private const string RepositoryName = "WinUI-SFTP-Browser";

    public UpdateCheckService()
    {
        _gitHubClient = new GitHubClient(new ProductHeaderValue("SFTP-Browser"));
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
            System.Diagnostics.Debug.WriteLine($"Error checking for updates: {ex.Message}");
            return null;
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
