using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Ardalis.GuardClauses;
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
    IClock clock
)
{
    private static readonly ConcurrentDictionary<string, Type> TypeCache = new();

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
        var publishTasks = messages
            .Select(message => PublishMessage(message, updateQueue, publisher, clock))
            .ToList();
        await Task.WhenAll(publishTasks);
        var publishTime = stepStopwatch.ElapsedMilliseconds;

        // 3. Mark messages as processed
        stepStopwatch.Restart();
        if (!updateQueue.IsEmpty)
        {
            await MarkMessagesAsProcessed(connection, transaction, updateQueue.ToList());
        }

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
            messageCount: messages.Count
        );
    }

    private static async Task MarkMessagesAsProcessed(NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        List<OutboxUpdate> updates)
    {
        // Create a single UPDATE statement using a VALUES list for efficiency

        var tuples = string.Join(
            ",",
            updates.Select((_, index) => $"(@Id{index}, @ProcessedOnUtc{index}, @Error{index})")
        );
        var updateSql =
            $"""
             UPDATE outbox_messages
             SET processed_on_utc = v.processed_on_utc,
                 error = v.error,
                 attempt_count = outbox_messages.attempt_count + 1
             FROM (VALUES {tuples}) AS v(id, processed_on_utc, error)
             WHERE outbox_messages.id = v.id::uuid
             """;

        var parameters = new DynamicParameters();
        for (var i = 0; i < updates.Count; i++)
        {
            var update = updates[i];
            parameters.Add($"Id{i}", update.Id);
            parameters.Add($"ProcessedOnUtc{i}", update.ProcessedOnUtc);
            parameters.Add($"Error{i}", update.Error);
        }

        await connection.ExecuteAsync(
            sql: updateSql,
            param: parameters,
            transaction: transaction
        );
    }

    private static async Task PublishMessage(OutboxMessage message,
        ConcurrentQueue<OutboxUpdate> updateQueue,
        IPublisher publisher,
        IClock clock
    )
    {
        try
        {
            var messageType = GetOrAddMessageType(message.Type);
            var deserialized = JsonSerializer.Deserialize(message.Content, messageType);
            Guard.Against.Null(deserialized,
                exceptionCreator: () => new InvalidOperationException("Failed to deserialize outbox message content."));

            await publisher.Publish(deserialized);

            updateQueue.Enqueue(new OutboxUpdate { Id = message.Id, ProcessedOnUtc = clock.UtcNow, Error = null });
        }
        catch (Exception ex)
        {
            updateQueue.Enqueue(new OutboxUpdate
            {
                Id = message.Id,
                ProcessedOnUtc = null,
                Error = ex.ToString()
            });
        }
    }

    private static Type GetOrAddMessageType(string typename) =>
        TypeCache.GetOrAdd(typename, k =>
        {
            var type = Domain.AssemblyReference.Assembly.GetType(k);
            return Guard.Against.Null(type);
        });

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
        Message = "An error occurred in OutboxMessagesProcessorJob")]
    internal static partial void LogError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "OutboxMessagesProcessorJob finished")]
    internal static partial void LogFinished(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message =
            "Outbox processing completed. Total time: {TotalTime}ms, Query time: {QueryTime}ms, Publish time: {PublishTime}ms, Update time: {UpdateTime}ms, Messages processed: {MessageCount}")]
    internal static partial void LogProcessingPerformance(ILogger logger, long totalTime, long queryTime,
        long publishTime, long updateTime, int messageCount);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "No outbox messages to process.")]
    internal static partial void LogNoMessagesToProcess(ILogger logger);
}
