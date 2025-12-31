// filepath: /Users/hoc.nguyen/Desktop/My/NetAuth/tests/UnitTests/Domain/Users/RefreshTokenTestData.cs

using NetAuth.Domain.Users;

namespace NetAuth.UnitTests.Domain.Users;

/// <summary>
/// Shared test data for RefreshToken-related tests.
/// Delegates to UserTestData for consistency.
/// </summary>
public static class RefreshTokenTestData
{
    #region Delegated from UserTestData

    public static Guid UserId => UserTestData.RefreshTokenUserId;

    public static Guid DeviceId => UserTestData.DeviceId;

    public static string TokenHash => UserTestData.TokenHash;

    public static DateTimeOffset CurrentUtc => UserTestData.CurrentUtc;

    public static DateTimeOffset FutureExpiration => UserTestData.FutureExpiration;

    public static DateTimeOffset PastExpiration => UserTestData.PastExpiration;

    #endregion

    #region Theory Data

    public static TheoryData<Guid> InvalidDeviceIds => UserTestData.InvalidDeviceIds;

    #endregion

    #region Factory Methods

    public static RefreshToken CreateRefreshToken(
        string? tokenHash = null,
        DateTimeOffset? expiresOnUtc = null,
        Guid? userId = null,
        Guid? deviceId = null) =>
        UserTestData.CreateRefreshToken(
            tokenHash: tokenHash,
            expiresOnUtc: expiresOnUtc,
            userId: userId,
            deviceId: deviceId);

    #endregion
}