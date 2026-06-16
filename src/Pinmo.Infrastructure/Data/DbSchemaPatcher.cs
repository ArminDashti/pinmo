using Microsoft.EntityFrameworkCore;
using Pinmo.Core;
using Pinmo.Infrastructure.Data;

namespace Pinmo.Infrastructure.Data;

public static class DbSchemaPatcher
{
    public static async Task ApplyAsync(PinmoDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        await TryAddColumnAsync(
            dbContext,
            "MonitoredEndpoints",
            "PacketsPerPing INTEGER NOT NULL DEFAULT 2;",
            cancellationToken);

        await TryAddColumnAsync(
            dbContext,
            "PingRecords",
            "PacketsSent INTEGER NOT NULL DEFAULT 1;",
            cancellationToken);

        await TryAddColumnAsync(
            dbContext,
            "PingRecords",
            "PacketsSucceeded INTEGER NOT NULL DEFAULT 0;",
            cancellationToken);

        await TryAddColumnAsync(
            dbContext,
            "AppSettings",
            "DefaultPacketsPerPing INTEGER NOT NULL DEFAULT 2;",
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            UPDATE AppSettings
            SET DefaultIntervalSeconds = 5
            WHERE DefaultIntervalSeconds NOT IN (1, 5, 10, 15, 30, 45, 60);
            """,
            cancellationToken);
    }

    private static async Task TryAddColumnAsync(
        PinmoDbContext dbContext,
        string tableName,
        string columnDefinition,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                $"ALTER TABLE {tableName} ADD COLUMN {columnDefinition}",
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
