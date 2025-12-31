// filepath: /Users/hoc.nguyen/Desktop/My/NetAuth/tests/UnitTests/Domain/Users/UserTestData.cs

using NetAuth.Domain.Users;

namespace NetAuth.UnitTests.Domain.Users;

/// <summary>
/// Shared test data for User-related tests.
/// </summary>
public static class UserTestData
{
    #region User Data

    public static readonly Username ValidUsername = Username.Create("valid-user").RightValueOrThrow();
    public static readonly Email ValidEmail = Email.Create("valid-user@gmail.com").RightValueOrThrow();

    public const string PlainPassword = "ValidPassword123@";
    public const string HashedPassword = "hashed_password_value";

    #endregion

    #region Common Timestamps

    /// <summary>
    /// Fixed point in time for consistent test results across all User tests.
    /// </summary>
    public static readonly DateTimeOffset CurrentUtc =
        new(year: 2025,
            month: 1,
            day: 1,
            hour: 12,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

    #endregion

    #region RefreshToken Data

    public static readonly Guid RefreshTokenUserId = Guid.NewGuid();
    public static readonly Guid DeviceId = Guid.NewGuid();

    public const string TokenHash = "hashed_token_value_12345";
    public const string RawRefreshToken = "raw-refresh-token-value";

    public static readonly DateTimeOffset FutureExpiration = CurrentUtc.AddDays(7);
    public static readonly DateTimeOffset PastExpiration = CurrentUtc.AddDays(-7);

    #endregion

    #region Theory Data

    public static TheoryData<IReadOnlyList<Role>?> InvalidRoles => [null, []];
    public static TheoryData<Guid> InvalidDeviceIds => [Guid.Empty];

    #endregion

    #region Factory Methods

    public static User CreateUser(
        Email? email = null,
        Username? username = null,
        string? passwordHash = null) =>
        User.Create(
            email: email ?? ValidEmail,
            username: username ?? ValidUsername,
            passwordHash: passwordHash ?? PlainPassword);

    public static RefreshToken CreateRefreshToken(
        string? tokenHash = null,
        DateTimeOffset? expiresOnUtc = null,
        Guid? userId = null,
        Guid? deviceId = null) =>
        RefreshToken.Create(
            tokenHash: tokenHash ?? TokenHash,
            expiresOnUtc: expiresOnUtc ?? FutureExpiration,
            userId: userId ?? RefreshTokenUserId,
            deviceId: deviceId ?? DeviceId);

    #endregion
}