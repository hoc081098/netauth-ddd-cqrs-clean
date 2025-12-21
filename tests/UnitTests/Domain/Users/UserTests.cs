using LanguageExt.UnitTesting;
using NetAuth.Domain.Users;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.UnitTests.Domain.Users;

public static class UserTestData
{
    public static readonly Username ValidUsername = Username.Create("valid-user").RightValueOrThrow();
    public static readonly Email ValidEmail = Email.Create("valid-user@gmail.com").RightValueOrThrow();
    public const string PlainPassword = "ValidPassword123@";
}

public class UserTests : BaseTest
{
    [Fact]
    public void CreateUser_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var email = UserTestData.ValidEmail;
        var username = UserTestData.ValidUsername;
        var passwordHash = UserTestData.PlainPassword; // Use plain password as hash for testing

        // Act
        var user = User.Create(email, username, passwordHash);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
        Assert.Equal(username, user.Username);
        Assert.Contains(Role.Member, user.Roles);

        var userCreatedDomainEvent = AssertDomainEventWasPublished<UserCreatedDomainEvent>(user);
        Assert.Equal(user.Id, userCreatedDomainEvent.UserId);
    }

    [Fact]
    public void VerifyPasswordHash_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var passwordHashChecker = new TestPasswordHashChecker();
        var email = UserTestData.ValidEmail;
        var username = UserTestData.ValidUsername;
        var password = UserTestData.PlainPassword;
        var passwordHash = UserTestData.PlainPassword; // Use plain password as hash for testing

        var user = User.Create(email, username, passwordHash);

        // Act
        var isMatch = user.VerifyPasswordHash(password: password,
            passwordHashChecker: passwordHashChecker);

        // Assert
        Assert.True(isMatch);
    }

    [Fact]
    public void VerifyPasswordHash_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var passwordHashChecker = new TestPasswordHashChecker();
        var email = UserTestData.ValidEmail;
        var username = UserTestData.ValidUsername;
        var password = UserTestData.PlainPassword;
        var passwordHash = UserTestData.PlainPassword; // Use plain password as hash for testing
        var user = User.Create(email, username, passwordHash);

        // Act
        var isMatch = user.VerifyPasswordHash(
            password: password + "Wrong",
            passwordHashChecker: passwordHashChecker);

        // Assert
        Assert.False(isMatch);
    }

    [Fact]
    public void Roles_ShouldBeReadOnly()
    {
        // Arrange
        var user = User.Create(
            UserTestData.ValidEmail,
            UserTestData.ValidUsername,
            UserTestData.PlainPassword);

        // Act & Assert
        Assert.IsType<IReadOnlyCollection<Role>>(user.Roles, exactMatch: false);
        Assert.ThrowsAny<NotSupportedException>(() => ((ICollection<Role>)user.Roles).Add(Role.Administrator));
    }

    public static TheoryData<IReadOnlyList<Role>?> InvalidRoles => [null, []];

    [Theory]
    [MemberData(nameof(InvalidRoles))]
    public void SetRoles_WithNullOrEmptyRoles_ShouldReturnDomainError(IReadOnlyList<Role>? roles)
    {
        // Arrange
        var user = User.Create(
            UserTestData.ValidEmail,
            UserTestData.ValidUsername,
            UserTestData.PlainPassword);

        // Act
        var result = user.SetRoles(roles: roles!, actor: RoleChangeActor.System);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.User.EmptyRolesNotAllowed, left));
    }
}