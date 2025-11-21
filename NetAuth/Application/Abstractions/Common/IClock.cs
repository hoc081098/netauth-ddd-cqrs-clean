namespace NetAuth.Application.Abstractions.Common;

/// <summary>
/// Abstraction for a clock to get the current time.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current date and time.
    /// </summary>
    public DateTimeOffset Now { get; }

    /// <summary>
    /// Get the current date and time in UTC.
    /// </summary>
    public DateTimeOffset UtcNow { get; }
}