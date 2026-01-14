using System.Diagnostics.CodeAnalysis;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using Quartz;

namespace NetAuth.Infrastructure.Outbox;

/// <summary>
/// Periodically deletes old, successfully processed outbox messages in small batches to avoid table bloat.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class OutboxCleanupJob(
    NpgsqlDataSource dataSource,
    ILogger<OutboxCleanupJob> logger,
    IOptions<OutboxSettings> outboxSettingsOptions
) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var settings = outboxSettingsOptions.Value;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var state = (totalDeleted: 0, batchCount: 0);
        while (state.batchCount < settings.MaxCleanupBatchesPerRun)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var deleted = await DeleteBatchAsync(connection, settings, cancellationToken);
            state = (state.totalDeleted + deleted, state.batchCount + 1);

            if (deleted < settings.CleanupBatchSize)
            {
                break;
            }

            // Optional: delay between cleanup batches to reduce DB load.
            if (settings.CleanupDelay > TimeSpan.Zero)
            {
                await Task.Delay(settings.CleanupDelay, cancellationToken);
            }
        }

        if (state.totalDeleted > 0)
        {
            OutboxCleanupLoggers.LogDeleted(logger, state.totalDeleted, state.batchCount);
        }
        else
        {
            OutboxCleanupLoggers.LogNoop(logger);
        }
    }

    [SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out")]
    private static Task<int> DeleteBatchAsync(
        NpgsqlConnection connection,
        OutboxSettings settings,
        CancellationToken cancellationToken)
    {
        // Must create an index on (occurred_on_utc) to optimize this query.
        // ```sql
        // CREATE INDEX idx_outbox_cleanup 
        // ON outbox_messages (occurred_on_utc, id) 
        // WHERE processed_on_utc IS NOT NULL AND error IS NULL;
        // ```

        return connection.ExecuteAsync(
            new CommandDefinition(
                commandText:
                """
                WITH deleted AS (
                    SELECT id
                    FROM outbox_messages
                    WHERE processed_on_utc IS NOT NULL
                        AND error IS NULL
                        AND occurred_on_utc < (now() - @Retention)
                    ORDER BY occurred_on_utc, id
                    LIMIT @BatchSize
                )
                DELETE FROM outbox_messages
                USING deleted
                WHERE outbox_messages.id = deleted.id;
                """,
                parameters: new
                {
                    Retention = settings.CleanupRetention,
                    BatchSize = settings.CleanupBatchSize
                },
                cancellationToken: cancellationToken
            )
        );
    }
}

internal static partial class OutboxCleanupLoggers
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Outbox cleanup deleted {DeletedCount} messages in {BatchCount} batches.")]
    internal static partial void LogDeleted(ILogger logger, int deletedCount, int batchCount);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Outbox cleanup found no messages to delete.")]
    internal static partial void LogNoop(ILogger logger);
}