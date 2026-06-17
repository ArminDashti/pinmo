using Pinmo.Core.Entities;
using Pinmo.Core.Interfaces;

namespace Pinmo.Infrastructure.Storage;

public sealed class JsonSettingsStore(string filePath) : ISettingsStore
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await ReadSettingsAsync(cancellationToken);
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
            var normalized = Normalize(settings);
            await JsonFileHelper.WriteAsync(filePath, normalized, cancellationToken);
            return normalized;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveIfMissingAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(filePath))
            {
                return;
            }

            await JsonFileHelper.WriteAsync(filePath, Normalize(settings), cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public bool FileExists() => File.Exists(filePath);

    private Task<AppSettings> ReadSettingsAsync(CancellationToken cancellationToken) =>
        JsonFileHelper.ReadAsync(filePath, CreateDefault(), cancellationToken);

    private static AppSettings CreateDefault() => new()
    {
        RequestTimeoutSeconds = 30
    };

    private static AppSettings Normalize(AppSettings settings)
    {
        settings.RequestTimeoutSeconds = Math.Clamp(settings.RequestTimeoutSeconds, 1, 120);

        if (!Enum.IsDefined(settings.CloseWindowAction))
        {
            settings.CloseWindowAction = CloseWindowAction.Quit;
        }

        return settings;
    }
}
