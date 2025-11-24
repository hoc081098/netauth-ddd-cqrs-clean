using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Ardalis.GuardClauses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Options;
using NetAuth.Application.Abstractions.Common;
using Npgsql;
using Quartz;

namespace NetAuth.Infrastructure.Outbox;

[DisallowConcurrentExecution]
internal sealed class OutboxMessagesProcessorJob(
    ILogger<OutboxMessagesProcessorJob> logger,
    IOptions<OutboxSettings> outboxSettingsOptions,
    NpgsqlDataSource dataSource,
    IPublisher publisher,
    IClock clock
) : IJob
{
    private static readonly ConcurrentDictionary<string, Type> TypeCache = new();

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Outbox Messages Processor Job Starting at {DateTime}", DateTimeOffset.Now);

        var totalStopwatch = Stopwatch.StartNew();

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var messages = (await connection.QueryAsync<OutboxMessage>(
            sql:
            """
            SELECT id AS "Id", type AS "Type", content AS "Content"
            FROM outbox_messages
            WHERE processed_on_utc IS NULL
            ORDER BY occurred_on_utc
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED
            """,
            param: new { outboxSettingsOptions.Value.BatchSize },
            transaction: transaction
        )).AsList();

        if (messages.Count == 0)
        {
            totalStopwatch.Stop();
            logger.LogInformation("Outbox Messages Processor Job Completed at {DateTime}, elapsed {TotalTime}",
                DateTimeOffset.Now,
                totalStopwatch.ElapsedMilliseconds);
            return;
        }

        var updateQueue = new ConcurrentQueue<OutboxUpdate>();
        var publishTasks = messages
            .Select(message => PublishMessage(message, updateQueue, publisher, clock))
            .ToList();
        await Task.WhenAll(publishTasks);

        if (!updateQueue.IsEmpty)
        {
            await MarkMessagesAsProcessed(connection, transaction, updateQueue.ToList());
        }

        await transaction.CommitAsync();

        totalStopwatch.Stop();
        logger.LogInformation("Outbox Messages Processor Job Completed at {DateTime}, elapsed {TotalTime}",
            DateTimeOffset.Now,
            totalStopwatch.ElapsedMilliseconds);
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
             SET processed_on_utc = v.processed_on_utc, error = v.error
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
                ProcessedOnUtc = clock.UtcNow,
                Error = ex.ToString()
            });
        }
    }

    private static Type GetOrAddMessageType(string typename) =>
        TypeCache.GetOrAdd(typename, name =>
            Guard.Against.Null(Domain.AssemblyReference.Assembly.GetType(name)));

    private class OutboxUpdate
    {
        public required Guid Id { get; init; }
        public required DateTimeOffset ProcessedOnUtc { get; init; }
        public required string? Error { get; init; }
    }
}

internal static partial class OutboxLoggers
{
    [LoggerMessage(Level = LogLevel.Information, Message = "OutboxBackgroundService starting...")]
    internal static partial void LogStarting(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting iteration {IterationCount}")]
    internal static partial void LogStartingIteration(ILogger logger, int iterationCount);

    [LoggerMessage(Level = LogLevel.Information,
        Message =
            "Iteration {IterationCount} completed. Processed {ProcessedMessages} messages. Total processed: {TotalProcessedMessages}")]
    internal static partial void LogIterationCompleted(ILogger logger, int iterationCount, int processedMessages,
        int totalProcessedMessages);

    [LoggerMessage(Level = LogLevel.Information, Message = "OutboxBackgroundService operation canceled.")]
    internal static partial void LogOperationCancelled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred in OutboxBackgroundService")]
    internal static partial void LogError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information,
        Message =
            "OutboxBackgroundService finished. Total iterations: {IterationCount}, Total processed messages: {TotalProcessedMessages}")]
    internal static partial void LogFinished(ILogger logger, int iterationCount, int totalProcessedMessages);

    [LoggerMessage(Level = LogLevel.Information,
        Message =
            "Outbox processing completed. Total time: {TotalTime}ms, Query time: {QueryTime}ms, Publish time: {PublishTime}ms, Update time: {UpdateTime}ms, Messages processed: {MessageCount}")]
    internal static partial void LogProcessingPerformance(ILogger logger, long totalTime, long queryTime,
        long publishTime, long updateTime, int messageCount);
}