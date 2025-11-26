using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Dapper;
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

    public async Task Execute()
    {
        var totalStopwatch = Stopwatch.StartNew();
        var stepStopwatch = new Stopwatch();

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var outboxSettings = outboxSettingsOptions.Value;

        // 1. Query unprocessed outbox messages
        stepStopwatch.Restart();
        var messages = (await connection.QueryAsync<OutboxMessage>(
            sql:
            """
            SELECT id AS "Id", type AS "Type", content AS "Content"
            FROM outbox_messages
            WHERE processed_on_utc IS NULL
                AND attempt_count < @MaxAttempts
            ORDER BY occurred_on_utc
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED
            """,
            param: new { outboxSettings.BatchSize, outboxSettings.MaxAttempts },
            transaction: transaction
        )).AsList();
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
            new ParallelOptions { MaxDegreeOfParallelism = MaxParallelism },
            (message, _) =>
                PublishMessage(message, updateQueue, publisher, clock, outboxMessageResolver, logger)
        );
        var publishTime = stepStopwatch.ElapsedMilliseconds;

        // 3. Mark messages as processed
        stepStopwatch.Restart();
        var rowsAffected = updateQueue switch
        {
            { IsEmpty: false } => await MarkMessagesAsProcessed(connection, transaction, updateQueue),
            _ => 0
        };
        var updateTime = stepStopwatch.ElapsedMilliseconds;

        // 4. Commit transaction
        await transaction.CommitAsync();

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

    private static async Task<int> MarkMessagesAsProcessed(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyCollection<OutboxUpdate> updates)
    {
        // Serialize all updates to JSON array once
        var updatesJson = JsonSerializer.Serialize(updates, SnakeCaseNamingJsonSerializerOptions);

        // Create a single UPDATE statement using a VALUES list for efficiency

        return await connection.ExecuteAsync(
            sql:
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
            param: new { updatesJson },
            transaction: transaction
        );
    }

    private static ValueTask PublishMessage(OutboxMessage message,
        ConcurrentQueue<OutboxUpdate> updateQueue,
        IPublisher publisher,
        IClock clock,
        IOutboxMessageResolver outboxMessageResolver,
        ILogger<OutboxProcessor> logger) =>
        outboxMessageResolver.DeserializeEvent(type: message.Type, content: message.Content)
            .Match<ValueTask>(
                Succ: async deserialized =>
                {
                    try
                    {
                        await publisher.Publish(deserialized);

                        updateQueue.Enqueue(new OutboxUpdate
                        {
                            Id = message.Id,
                            ProcessedOnUtc = clock.UtcNow,
                            Error = null
                        });
                    }
                    catch (Exception ex)
                    {
                        // Publishing failed. Log the error and do not mark the message as processed to allow for retries.
                        OutboxMessagesProcessorLoggers.LogError(logger, ex);

                        updateQueue.Enqueue(new OutboxUpdate
                        {
                            Id = message.Id,
                            ProcessedOnUtc = null,
                            Error = ex.ToString()
                        });
                    }
                },
                Fail: error =>
                {
                    // Keep for retry until MaxAttempts; record the error.
                    OutboxMessagesProcessorLoggers.LogError(logger, error);

                    updateQueue.Enqueue(new OutboxUpdate
                    {
                        Id = message.Id,
                        ProcessedOnUtc = null,
                        Error = error.ToString()
                    });

                    return ValueTask.CompletedTask;
                }
            );

    private class OutboxUpdate
    {
        public required Guid Id { get; init; }
        public required DateTimeOffset? ProcessedOnUtc { get; init; }
        public required string? Error { get; init; }
    }
}

internal static partial class OutboxMessagesProcessorLoggers
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "OutboxMessagesProcessorJob starting...")]
    internal static partial void LogStarting(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Starting iteration {IterationCount}")]
    internal static partial void LogStartingIteration(ILogger logger, int iterationCount);

    [LoggerMessage(Level = LogLevel.Information,
        Message =
            "Iteration {IterationCount} completed. Processed {ProcessedMessages} messages. Total processed: {TotalProcessedMessages}")]
    internal static partial void LogIterationCompleted(ILogger logger, int iterationCount, int processedMessages,
        int totalProcessedMessages);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "An error occurred in OutboxProcessor")]
    internal static partial void LogError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "OutboxMessagesProcessorJob finished")]
    internal static partial void LogFinished(ILogger logger);

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