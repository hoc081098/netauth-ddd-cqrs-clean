namespace NetAuth.Application.Abstractions.Common;

/// <summary>
/// Abstraction for a clock to get the current time.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Get the current date and time in UTC.
    /// </summary>
    public DateTimeOffset UtcNow { get; }
}