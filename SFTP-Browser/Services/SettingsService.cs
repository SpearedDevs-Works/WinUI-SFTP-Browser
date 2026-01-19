using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using SFTP_Browser.Models;

namespace SFTP_Browser.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    private static string SettingsPath
        => Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings.json");

    public async Task<AppSettingsModel> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettingsModel();

            var json = await File.ReadAllTextAsync(SettingsPath, cancellationToken);
            return JsonSerializer.Deserialize<AppSettingsModel>(json, _jsonOptions) ?? new AppSettingsModel();
        }
        catch
        {
            return new AppSettingsModel();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(AppSettingsModel settings, CancellationToken cancellationToken = default)
    {
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(SettingsPath, json, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}
