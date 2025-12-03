using System.ComponentModel.DataAnnotations;

namespace NetAuth.Infrastructure.Outbox;

/// <summary>
/// Represents the Outbox configuration settings for background job processing.
/// </summary>
internal sealed record OutboxSettings
{
    public const string SectionKey = "Outbox";

    /// <summary>
    /// The interval between background job executions.
    /// </summary>
    public required TimeSpan Interval { get; init; }

    /// <summary>
    /// The number of outbox messages that we are processing in one run of the background job.
    /// </summary>
    [Range(minimum: 1, maximum: int.MaxValue, ErrorMessage = "BatchSize must be greater than or equal to 1.")]
    public required int BatchSize { get; init; }

    /// <summary>
    /// Maximum number of attempts before parking a message.
    /// </summary>
    [Range(minimum: 1, maximum: 100, ErrorMessage = "MaxAttempts must be between 1 and 100.")]
    public required int MaxAttempts { get; init; }

    /// <summary>
    /// How long to keep successfully processed outbox messages.
    /// </summary>
    public required TimeSpan CleanupRetention { get; init; }

    /// <summary>
    /// Maximum rows to delete per cleanup batch.
    /// </summary>
    [Range(minimum: 1, maximum: 10_000, ErrorMessage = "CleanupBatchSize must be between 1 and 10_000.")]
    public required int CleanupBatchSize { get; init; }
}