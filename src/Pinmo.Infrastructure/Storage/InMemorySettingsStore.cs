using Pinmo.Core.Entities;
using Pinmo.Core.Interfaces;

namespace Pinmo.Infrastructure.Storage;

public sealed class InMemorySettingsStore : ISettingsStore
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private AppSettings _settings = CreateDefault();

    public async Task LoadSeedFromFileAsync(string? filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        var seeded = await JsonFileHelper.ReadAsync(filePath, CreateDefault(), cancellationToken);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _settings = Normalize(seeded);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return Clone(_settings);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<AppSettings> SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _settings = Normalize(settings);
            return Clone(_settings);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static AppSettings CreateDefault() => new()
    {
        RequestTimeoutSeconds = 30
    };

    private static AppSettings Normalize(AppSettings settings)
    {
        var normalized = Clone(settings);
        normalized.RequestTimeoutSeconds = Math.Clamp(normalized.RequestTimeoutSeconds, 1, 120);
        return normalized;
    }

    private static AppSettings Clone(AppSettings settings) => new()
    {
        RequestTimeoutSeconds = settings.RequestTimeoutSeconds,
        LaunchAtStartup = settings.LaunchAtStartup
    };
}
