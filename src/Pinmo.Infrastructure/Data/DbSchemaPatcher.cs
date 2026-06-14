using Microsoft.EntityFrameworkCore;
using Pinmo.Infrastructure.Data;

namespace Pinmo.Infrastructure.Data;

public static class DbSchemaPatcher
{
    public static async Task ApplyAsync(PinmoDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        await TryAddPacketsPerPingColumnAsync(dbContext, cancellationToken);
    }

    private static async Task TryAddPacketsPerPingColumnAsync(
        PinmoDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE MonitoredEndpoints ADD COLUMN PacketsPerPing INTEGER NOT NULL DEFAULT 2;",
                cancellationToken);
        }
        catch (Exception ex) when (IsDuplicateColumnError(ex))
        {
            // Column already exists on upgraded databases.
        }
    }

    private static bool IsDuplicateColumnError(Exception ex)
    {
        var message = ex.Message;
        return message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)
            || message.Contains("already exists", StringComparison.OrdinalIgnoreCase);
    }
}
