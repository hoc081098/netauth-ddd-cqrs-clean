using NetAuth.Domain.Users;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.UnitTests.Domain.Users;

public static class UserTestData
{
    public static readonly Username ValidUsername = Username.Create("valid-user").RightValueOrThrow();
    public static readonly Email ValidEmail = Email.Create("valid-user@gmail.com").RightValueOrThrow();
    public const string PasswordHash = "ValidPasswordHash123@";
}

public class UserTests : BaseTest
{
    [Fact]
    public void CreateUser_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var email = UserTestData.ValidEmail;
        var username = UserTestData.ValidUsername;

        // Act
        var user = User.Create(email, username, UserTestData.PasswordHash);

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
        var passwordHash = UserTestData.PasswordHash;

        var user = User.Create(email, username, passwordHash);

        // Act
        var isMatch = user.VerifyPasswordHash(password: passwordHash,
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

        var user = User.Create(email, username, UserTestData.PasswordHash);

        // Act
        var isMatch = user.VerifyPasswordHash(
            password: UserTestData.PasswordHash + "Wrong",
            passwordHashChecker: passwordHashChecker);

        // Assert
        Assert.False(isMatch);
    }
}