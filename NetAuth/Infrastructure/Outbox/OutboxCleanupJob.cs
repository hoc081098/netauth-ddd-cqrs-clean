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

        var totalDeleted = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var deleted = await DeleteBatchAsync(connection, settings, cancellationToken);
            totalDeleted += deleted;

            if (deleted == 0)
            {
                break;
            }
        }

        if (totalDeleted > 0)
        {
            OutboxCleanupLoggers.LogDeleted(logger, totalDeleted);
        }
    }

    private static Task<int> DeleteBatchAsync(
        NpgsqlConnection connection,
        OutboxSettings settings,
        CancellationToken cancellationToken)
    {
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
                    LIMIT @BatchSize
                )
                DELETE FROM outbox_messages
                USING deleted
                WHERE outbox_messages.id = deleted.id;
                """,
                parameters: new
                {
                    Retention = TimeSpan.FromDays(settings.CleanupRetentionDays),
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
        Message = "Outbox cleanup deleted {DeletedCount} messages.")]
    internal static partial void LogDeleted(ILogger logger, int deletedCount);
}