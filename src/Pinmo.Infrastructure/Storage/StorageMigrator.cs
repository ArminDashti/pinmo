using Microsoft.EntityFrameworkCore;
using Pinmo.Core.Interfaces;
using Pinmo.Infrastructure.Data;

namespace Pinmo.Infrastructure.Storage;

public static class StorageMigrator
{
    public static async Task MigrateFromDatabaseIfNeededAsync(
        PinmoDbContext dbContext,
        JsonEndpointStore endpointStore,
        JsonSettingsStore settingsStore,
        CancellationToken cancellationToken = default)
    {
        var legacyEndpoints = await TryLoadLegacyEndpointsAsync(dbContext, cancellationToken);
        if (legacyEndpoints.Count > 0)
        {
            var currentEndpoints = await endpointStore.GetAllAsync(cancellationToken);
            if (currentEndpoints.Count == 0)
            {
                await endpointStore.ReplaceAllAsync(legacyEndpoints, cancellationToken);
            }
        }

        if (!settingsStore.FileExists())
        {
            var legacySettings = await TryLoadLegacySettingsAsync(dbContext, cancellationToken);
            if (legacySettings is not null)
            {
                await settingsStore.SaveIfMissingAsync(legacySettings, cancellationToken);
            }
        }

        await settingsStore.SaveIfMissingAsync(new(), cancellationToken);
    }

    private static async Task<List<Core.Entities.MonitoredEndpoint>> TryLoadLegacyEndpointsAsync(
        PinmoDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.MonitoredEndpoints
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        catch
        {
            return [];
        }
    }

    private static async Task<Core.Entities.AppSettings?> TryLoadLegacySettingsAsync(
        PinmoDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.AppSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
