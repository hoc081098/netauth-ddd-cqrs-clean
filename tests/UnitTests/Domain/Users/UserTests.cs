using LanguageExt.UnitTesting;
using NetAuth.Domain.Users;
using NetAuth.Domain.Users.DomainEvents;

namespace NetAuth.UnitTests.Domain.Users;

public static class UserTestData
{
    public const string ValidUsername = "valid-user";
    public const string ValidEmail = "valid-user@gmail.com";
    public const string PasswordHash = "ValidPasswordHash123@";
}

public class UserTests : BaseTest
{
    [Fact]
    public void CreateUser_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var usernameResult = Username.Create(UserTestData.ValidUsername);
        var emailResult = Email.Create(UserTestData.ValidEmail);

        usernameResult.ShouldBeRight(username =>
        {
            emailResult.ShouldBeRight(email =>
            {
                // Act
                var user = User.Create(email, username, UserTestData.PasswordHash);

                // Assert
                Assert.NotNull(user);
                Assert.Equal(email, user.Email);
                Assert.Equal(username, user.Username);
                Assert.Contains(Role.Member, user.Roles);

                var userCreatedDomainEvent = AssertDomainEventWasPublished<UserCreatedDomainEvent>(user);
                Assert.Equal(user.Id, userCreatedDomainEvent.UserId);
            });
        });
    }

    [Fact]
    public void VerifyPasswordHash_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var passwordHashChecker = new TestPasswordHashChecker();

        var usernameResult = Username.Create(UserTestData.ValidUsername);
        var emailResult = Email.Create(UserTestData.ValidEmail);

        usernameResult.ShouldBeRight(username =>
        {
            emailResult.ShouldBeRight(email =>
            {
                var user = User.Create(email, username, UserTestData.PasswordHash);

                // Act
                var isMatch = user.VerifyPasswordHash(password: UserTestData.PasswordHash,
                    passwordHashChecker: passwordHashChecker);

                // Assert
                Assert.True(isMatch);
            });
        });
    }

    [Fact]
    public void VerifyPasswordHash_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var passwordHashChecker = new TestPasswordHashChecker();

        var usernameResult = Username.Create(UserTestData.ValidUsername);
        var emailResult = Email.Create(UserTestData.ValidEmail);

        usernameResult.ShouldBeRight(username =>
        {
            emailResult.ShouldBeRight(email =>
            {
                var user = User.Create(email, username, UserTestData.PasswordHash);

                // Act
                var isMatch = user.VerifyPasswordHash(
                    password: UserTestData.PasswordHash + "Wrong",
                    passwordHashChecker: passwordHashChecker);

                // Assert
                Assert.False(isMatch);
            });
        });
    }
}