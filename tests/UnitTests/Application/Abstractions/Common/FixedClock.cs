using NetAuth.Application.Abstractions.Common;

namespace NetAuth.UnitTests.Application.Abstractions.Common;

public class FixedClock : IClock
{
    public required DateTimeOffset UtcNow { get; init; }

    public static FixedClock CreateWithUtcNow(DateTimeOffset fixedTime)
    {
        // Check that the provided time is in UTC
        if (fixedTime.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("The fixed time must be in UTC.", nameof(fixedTime));
        }

        return new FixedClock
        {
            UtcNow = fixedTime
        };
    }
}