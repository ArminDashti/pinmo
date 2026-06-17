using Microsoft.EntityFrameworkCore;
using Pinmo.Infrastructure.Data;

namespace Pinmo.Infrastructure.Data;

public static class DbSchemaPatcher
{
    public static async Task ApplyAsync(PinmoDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

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
