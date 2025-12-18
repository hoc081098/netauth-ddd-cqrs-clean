namespace NetAuth.Infrastructure.Outbox;

internal sealed class OutboxMessage
{
    public required Guid Id { get; init; }
    
    /// <summary>
    /// The type of the outbox message.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The serialized content of the outbox message in JSON format.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The UTC date and time when the outbox message was created.
    /// </summary>
    public required DateTimeOffset OccurredOnUtc { get; init; }

    /// <summary>
    /// The UTC date and time when the outbox message was processed.
    /// If null, the message has not been processed yet.
    /// </summary>
    public DateTimeOffset? ProcessedOnUtc { get; init; }

    /// <summary>
    /// The error message if processing the outbox message failed.
    /// If null, the message was processed successfully.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// The number of attempts made to process the outbox message.
    /// </summary>
    public int AttemptCount { get; init; }
}