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
}