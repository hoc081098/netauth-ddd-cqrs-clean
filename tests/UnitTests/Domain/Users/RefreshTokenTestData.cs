using NetAuth.Domain.Users;

namespace NetAuth.UnitTests.Domain.Users;

/// <summary>
/// Shared test data for RefreshToken-related tests.
/// </summary>
public static class RefreshTokenTestData
{
    #region RefreshToken Data

    public static readonly Guid UserId = Guid.NewGuid();
    public static readonly Guid DeviceId = Guid.NewGuid();

    public const string TokenHash = "hashed_token_value_12345";
    public const string RawRefreshToken = "raw-refresh-token-value";

    #endregion

    #region Common Timestamps

    /// <summary>
    /// Fixed point in time for consistent test results across all RefreshToken tests.
    /// </summary>
    public static readonly DateTimeOffset CurrentUtc =
        new(year: 2025,
            month: 1,
            day: 1,
            hour: 12,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

    public static readonly DateTimeOffset FutureExpiration = CurrentUtc.AddDays(7);
    public static readonly DateTimeOffset PastExpiration = CurrentUtc.AddDays(-7);

    #endregion

    #region Theory Data

    public static TheoryData<Guid> InvalidDeviceIds => [Guid.Empty];

    #endregion

    #region Factory Methods

    public static RefreshToken CreateRefreshToken(
        string? tokenHash = null,
        DateTimeOffset? expiresOnUtc = null,
        Guid? userId = null,
        Guid? deviceId = null) =>
        RefreshToken.Create(
            tokenHash: tokenHash ?? TokenHash,
            expiresOnUtc: expiresOnUtc ?? FutureExpiration,
            userId: userId ?? UserId,
            deviceId: deviceId ?? DeviceId);

    #endregion
}