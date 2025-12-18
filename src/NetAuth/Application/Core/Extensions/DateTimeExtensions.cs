using System.Diagnostics;

namespace NetAuth.Application.Core.Extensions;

public static class DateTimeExtensions
{
    [DebuggerStepThrough]
    public static DateTimeOffset StartOfDay(this DateTimeOffset time) =>
        new(year: time.Year,
            month: time.Month,
            day: time.Day,
            hour: 0,
            minute: 0,
            second: 0,
            offset: time.Offset);
}