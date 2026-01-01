using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Dapper;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Options;
using NetAuth.Application.Abstractions.Common;
using Npgsql;

namespace NetAuth.Infrastructure.Outbox;

internal sealed class OutboxProcessor(
    ILogger<OutboxProcessor> logger,
    IOptions<OutboxSettings> outboxSettingsOptions,
    NpgsqlDataSource dataSource,
    IPublisher publisher,
    IClock clock,
    IOutboxMessageResolver outboxMessageResolver
)
{
    private static readonly JsonSerializerOptions SnakeCaseNamingJsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private const int MaxParallelism = 5;

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var stepStopwatch = new Stopwatch();

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var outboxSettings = outboxSettingsOptions.Value;

        // 1. Query unprocessed outbox messages
        stepStopwatch.Restart();
        var messages = (
            await connection.QueryAsync<OutboxMessage>(
                new CommandDefinition(
                    commandText:
                    """
                    SELECT id AS "Id", type AS "Type", content AS "Content"
                    FROM outbox_messages
                    WHERE processed_on_utc IS NULL
                        AND attempt_count < @MaxAttempts
                    ORDER BY occurred_on_utc
                    LIMIT @BatchSize
                    FOR UPDATE SKIP LOCKED
                    """,
                    parameters: new { outboxSettings.BatchSize, outboxSettings.MaxAttempts },
                    transaction: transaction,
                    cancellationToken: cancellationToken
                )
            )
        ).AsList();
        cancellationToken.ThrowIfCancellationRequested();
        var queryTime = stepStopwatch.ElapsedMilliseconds;

        if (messages.Count == 0)
        {
            OutboxMessagesProcessorLoggers.LogNoMessagesToProcess(logger);
            return;
        }

        // 2. Publish messages concurrently
        stepStopwatch.Restart();
        var updateQueue = new ConcurrentQueue<OutboxUpdate>();
        await Parallel.ForEachAsync(
            messages,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxParallelism,
                CancellationToken = cancellationToken
            },
            (message, token) =>
                PublishMessage(message,
                    updateQueue,
                    publisher,
                    clock,
                    outboxMessageResolver,
                    logger,
                    token
                )
        );
        var publishTime = stepStopwatch.ElapsedMilliseconds;

        // 3. Mark messages as processed
        stepStopwatch.Restart();
        var rowsAffected = updateQueue switch
        {
            { IsEmpty: false } =>
                await MarkMessagesAsProcessed(connection,
                    transaction,
                    updateQueue,
                    cancellationToken),
            _ => 0
        };
        cancellationToken.ThrowIfCancellationRequested();
        var updateTime = stepStopwatch.ElapsedMilliseconds;

        // 4. Commit transaction
        await transaction.CommitAsync(cancellationToken);

        totalStopwatch.Stop();
        OutboxMessagesProcessorLoggers.LogProcessingPerformance(
            logger: logger,
            totalTime: totalStopwatch.ElapsedMilliseconds,
            queryTime: queryTime,
            publishTime: publishTime,
            updateTime: updateTime,
            messageCount: messages.Count,
            rowsAffected: rowsAffected
        );
    }

    private static async ValueTask<int> MarkMessagesAsProcessed(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyCollection<OutboxUpdate> updates,
        CancellationToken cancellationToken = default)
    {
        // Serialize all updates to JSON array once
        var updatesJson = JsonSerializer.Serialize(updates, SnakeCaseNamingJsonSerializerOptions);

        // Create a single UPDATE statement using `json_to_recordset` for efficient bulk update
        return await connection.ExecuteAsync(
            new CommandDefinition(
                commandText:
                """
                WITH
                    data AS (
                        SELECT
                            id,
                            processed_on_utc,
                            error
                        FROM
                            json_to_recordset(@updatesJson::json) AS x (id UUID, processed_on_utc timestamptz, error text)
                    )
                UPDATE outbox_messages AS m
                SET
                    processed_on_utc = data.processed_on_utc,
                    error = data.error,
                    attempt_count = CASE
                        WHEN data.error IS NULL THEN m.attempt_count
                        ELSE m.attempt_count + 1
                    END
                FROM data
                WHERE m.id = data.id;
                """,
                parameters: new { updatesJson },
                transaction: transaction,
                cancellationToken: cancellationToken
            )
        );
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private static ValueTask PublishMessage(OutboxMessage message,
        ConcurrentQueue<OutboxUpdate> updateQueue,
        IPublisher publisher,
        IClock clock,
        IOutboxMessageResolver outboxMessageResolver,
        ILogger<OutboxProcessor> logger,
        CancellationToken cancellationToken = default
    ) =>
        outboxMessageResolver.DeserializeEvent(type: message.Type, content: message.Content)
            .Match<ValueTask>(
                Succ: async deserialized =>
                {
                    try
                    {
                        await publisher.Publish(deserialized, cancellationToken);
                        updateQueue.Enqueue(
                            new OutboxUpdate(Id: message.Id,
                                ProcessedOnUtc: clock.UtcNow,
                                Error: null));
                    }
                    catch (Exception ex)
                    {
                        // Publishing failed.
                        OutboxMessagesProcessorLoggers.LogFailedToPublish(logger, ex, message.Id);
                        // Do not mark the message as processed to allow for retries.
                        updateQueue.Enqueue(
                            new OutboxUpdate(Id: message.Id,
                                ProcessedOnUtc: null,
                                Error: ex.ToString()));
                    }
                },
                Fail: error =>
                {
                    // Deserialization failed.
                    OutboxMessagesProcessorLoggers.LogFailedToDeserialize(logger, error, message.Id);
                    // Mark the message as processed with the error and do not retry.
                    updateQueue.Enqueue(
                        new OutboxUpdate(Id: message.Id,
                            ProcessedOnUtc: clock.UtcNow,
                            Error: error.ToString()));

                    return ValueTask.CompletedTask;
                }
            );

    [UsedImplicitly]
    private readonly record struct OutboxUpdate(
        Guid Id,
        DateTimeOffset? ProcessedOnUtc,
        string? Error
    );
}

internal static partial class OutboxMessagesProcessorLoggers
{
    [LoggerMessage(Level = LogLevel.Error,
        Message = "Failed to publish outbox message {OutboxMessageId}")]
    internal static partial void LogFailedToPublish(ILogger logger, Exception exception, Guid outboxMessageId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Failed to deserialize outbox message {OutboxMessageId}")]
    internal static partial void LogFailedToDeserialize(ILogger logger, Exception exception, Guid outboxMessageId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "An error occurred in OutboxProcessor")]
    internal static partial void LogError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "OutboxProcessor cancelled")]
    internal static partial void LogCancelled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message =
            "Outbox processing completed. Total time: {TotalTime}ms, Query time: {QueryTime}ms," +
            " Publish time: {PublishTime}ms, Update time: {UpdateTime}ms, Messages processed: {MessageCount}," +
            " Rows affected: {RowsAffected}")]
    internal static partial void LogProcessingPerformance(ILogger logger,
        long totalTime,
        long queryTime,
        long publishTime,
        long updateTime,
        int messageCount,
        int rowsAffected);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "No outbox messages to process.")]
    internal static partial void LogNoMessagesToProcess(ILogger logger);
}