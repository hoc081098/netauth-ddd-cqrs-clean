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

    #region Theory Data

    public static TheoryData<IReadOnlyList<Role>?> InvalidRoles => [null, []];

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

    #endregion
}