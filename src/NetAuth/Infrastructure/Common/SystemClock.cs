using NetAuth.Application.Abstractions.Common;

namespace NetAuth.Infrastructure.Common;

/// <summary>
/// Implementation of <see cref="IClock"/> that uses the system clock.
/// </summary>
internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}