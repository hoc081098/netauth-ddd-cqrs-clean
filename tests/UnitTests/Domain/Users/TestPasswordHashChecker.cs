using NetAuth.Domain.Users;

namespace NetAuth.UnitTests.Domain.Users;

/// <summary>
/// A test implementation of IPasswordHashChecker that checks for exact string match.
/// </summary>
public class TestPasswordHashChecker : IPasswordHashChecker
{
    public bool IsMatch(string passwordHash, string providedPassword) =>
        passwordHash == providedPassword;
}