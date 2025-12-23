using NetAuth.Application.Abstractions.Common;

namespace NetAuth.UnitTests.Application.Abstractions.Common;

public class FixedClock : IClock
{
    public required DateTimeOffset Now { get; init; }
    public required DateTimeOffset UtcNow { get; init; }

    public static FixedClock Create(DateTimeOffset fixedTime) =>
        new()
        {
            Now = fixedTime,
            UtcNow = fixedTime.ToUniversalTime()
        };
}