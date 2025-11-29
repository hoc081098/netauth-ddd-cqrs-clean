namespace NetAuth.Infrastructure.Outbox;

/// <summary>
/// Used for background jobs to process the messages
/// </summary>
internal sealed record OutboxSettings
{
    public const string SectionKey = "Outbox";
    
    public required TimeSpan Interval { get; init; }

    /// <summary>
    /// The number of outbox messages that we are processing in one run of the background job
    /// </summary>
    public required int BatchSize { get; init; }

    /// <summary>
    /// Maximum number of attempts before parking a message.
    /// </summary>
    public required int MaxAttempts { get; init; }

    /// <summary>
    /// How many days to keep successfully processed outbox messages.
    /// </summary>
    public int CleanupRetentionDays { get; init; } = 30;

    /// <summary>
    /// Maximum rows to delete per cleanup batch.
    /// </summary>
    public int CleanupBatchSize { get; init; } = 5_000;
}
