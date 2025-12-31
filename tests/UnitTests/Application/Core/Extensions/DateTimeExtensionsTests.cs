using NetAuth.Application.Core.Extensions;

namespace NetAuth.UnitTests.Application.Core.Extensions;

public class DateTimeExtensionsTests
{
    [Fact]
    public void StartOfDay_ShouldReturnMidnight()
    {
        // Arrange
        var dateTime = new DateTimeOffset(year: 2025,
            month: 12,
            day: 31,
            hour: 14,
            minute: 30,
            second: 45,
            offset: TimeSpan.Zero);

        // Act
        var result = dateTime.StartOfDay();

        // Assert
        Assert.Equal(2025, result.Year);
        Assert.Equal(12, result.Month);
        Assert.Equal(31, result.Day);
        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
        Assert.Equal(TimeSpan.Zero, result.Offset);
    }

    [Fact]
    public void StartOfDay_ShouldPreserveOffset()
    {
        // Arrange
        var offset = TimeSpan.FromHours(7);
        var dateTime = new DateTimeOffset(year: 2025,
            month: 6,
            day: 15,
            hour: 23,
            minute: 59,
            second: 59,
            offset: offset);

        // Act
        var result = dateTime.StartOfDay();

        // Assert
        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
        Assert.Equal(offset, result.Offset);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(12, 30, 15)]
    [InlineData(23, 59, 59)]
    public void StartOfDay_WithVariousTimes_ShouldAlwaysReturnMidnight(int hour, int minute, int second)
    {
        // Arrange
        var dateTime = new DateTimeOffset(year: 2025,
            month: 1,
            day: 15,
            hour: hour,
            minute: minute,
            second: second,
            offset: TimeSpan.Zero);

        // Act
        var result = dateTime.StartOfDay();

        // Assert
        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    [Fact]
    public void StartOfDay_AtMidnight_ShouldReturnSameValue()
    {
        // Arrange
        var midnight = new DateTimeOffset(year: 2025,
            month: 7,
            day: 4,
            hour: 0,
            minute: 0,
            second: 0,
            offset: TimeSpan.FromHours(-5));

        // Act
        var result = midnight.StartOfDay();

        // Assert
        Assert.Equal(midnight, result);
    }

    [Theory]
    [InlineData(-12)]
    [InlineData(-5)]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(12)]
    public void StartOfDay_WithVariousOffsets_ShouldPreserveOffset(int offsetHours)
    {
        // Arrange
        var offset = TimeSpan.FromHours(offsetHours);
        var dateTime = new DateTimeOffset(year: 2025,
            month: 3,
            day: 20,
            hour: 15,
            minute: 45,
            second: 30,
            offset: offset);

        // Act
        var result = dateTime.StartOfDay();

        // Assert
        Assert.Equal(offset, result.Offset);
        Assert.Equal(2025, result.Year);
        Assert.Equal(3, result.Month);
        Assert.Equal(20, result.Day);
    }

    [Fact]
    public void StartOfDay_ShouldPreserveDate()
    {
        // Arrange
        var dateTime = new DateTimeOffset(year: 2020,
            month: 2,
            day: 29,
            hour: 18,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero); // Leap year

        // Act
        var result = dateTime.StartOfDay();

        // Assert
        Assert.Equal(2020, result.Year);
        Assert.Equal(2, result.Month);
        Assert.Equal(29, result.Day);
    }

    [Fact]
    public void StartOfDay_WithNegativeOffset_ShouldWork()
    {
        // Arrange
        var offset = TimeSpan.FromHours(-8); // Pacific Time
        var dateTime = new DateTimeOffset(year: 2025,
            month: 12,
            day: 25,
            hour: 6,
            minute: 30,
            second: 0,
            offset: offset);

        // Act
        var result = dateTime.StartOfDay();

        // Assert
        Assert.Equal(
            new DateTimeOffset(year: 2025,
                month: 12,
                day: 25,
                hour: 0,
                minute: 0,
                second: 0,
                offset: offset),
            result);
    }
}