using LanguageExt.UnitTesting;
using NetAuth.Domain.Users;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace NetAuth.UnitTests.Domain.Users;

public class UsernameTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData("user123")]
    [InlineData("john_doe")]
    [InlineData("test-user")]
    [InlineData("User_Name-123")]
    [InlineData("a1_")]
    [InlineData("USERNAME")]
    [InlineData("user_name_123")]
    [InlineData("test-user-name")]
    public void Create_WithValidUsername_ShouldReturnSuccess(string validUsername)
    {
        // Act
        var result = Username.Create(validUsername);

        // Assert
        result.ShouldBeRight(right =>
        {
            Assert.Equal(validUsername, right.Value);
            Assert.Equal(validUsername, (string)right); // Test implicit conversion
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Create_WithNullOrWhiteSpace_ShouldReturnNullOrEmptyError(string? invalidUsername)
    {
        // Act
        var result = Username.Create(invalidUsername!);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Username.NullOrEmpty, left));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("12")]
    public void Create_WithUsernameTooShort_ShouldReturnTooShortError(string shortUsername)
    {
        // Act
        var result = Username.Create(shortUsername);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Username.TooShort, left));
    }

    [Fact]
    public void Create_WithUsernameExceedingMaxLength_ShouldReturnTooLongError()
    {
        // Arrange
        var longUsername = new string('a', Username.MaxLength + 1);

        // Act
        var result = Username.Create(longUsername);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Username.TooLong, left));
    }

    [Theory]
    [InlineData("user name")]
    [InlineData("user@name")]
    [InlineData("user.name")]
    [InlineData("user#name")]
    [InlineData("user$name")]
    [InlineData("user%name")]
    [InlineData("user&name")]
    [InlineData("user*name")]
    [InlineData("user+name")]
    [InlineData("user=name")]
    [InlineData("user!name")]
    [InlineData("user?name")]
    [InlineData("user/name")]
    [InlineData("user\\name")]
    [InlineData("user,name")]
    [InlineData("user;name")]
    [InlineData("user:name")]
    [InlineData("user'name")]
    [InlineData("user\"name")]
    [InlineData("user<name")]
    [InlineData("user>name")]
    [InlineData("user[name")]
    [InlineData("user]name")]
    [InlineData("user{name")]
    [InlineData("user}name")]
    [InlineData("user(name")]
    [InlineData("user)name")]
    [InlineData("user|name")]
    [InlineData("user~name")]
    [InlineData("user`name")]
    public void Create_WithInvalidFormat_ShouldReturnInvalidFormatError(string invalidUsername)
    {
        // Act
        var result = Username.Create(invalidUsername);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Username.InvalidFormat, left));
    }

    [Fact]
    public void Equals_WithSameUsernameValue_ShouldReturnTrue()
    {
        // Arrange
        var username1Result = Username.Create("testuser");
        var username2Result = Username.Create("testuser");

        // Act & Assert
        username1Result.ShouldBeRight(username1 =>
        {
            username2Result.ShouldBeRight(username2 =>
            {
                Assert.Equal(username1, username2);
                Assert.True(username1.Equals(username2));
                Assert.True(username1.Equals((object?)username2));
                Assert.True(username1 == username2);
                Assert.False(username1 != username2);
            });
        });
    }

    [Fact]
    public void Equals_WithDifferentUsernameValue_ShouldReturnFalse()
    {
        // Arrange
        var username1Result = Username.Create("user1");
        var username2Result = Username.Create("user2");

        // Act & Assert
        username1Result.ShouldBeRight(username1 =>
        {
            username2Result.ShouldBeRight(username2 =>
            {
                Assert.NotEqual(username1, username2);
                Assert.False(username1.Equals(username2));
                Assert.False(username1.Equals((object?)username2));
                Assert.False(username1 == username2);
                Assert.True(username1 != username2);
            });
        });
    }

    [Fact]
    public void GetHashCode_WithSameUsernameValue_ShouldReturnSameHashCode()
    {
        // Arrange
        var username1Result = Username.Create("testuser");
        var username2Result = Username.Create("testuser");

        // Act & Assert
        username1Result.ShouldBeRight(username1 =>
        {
            username2Result.ShouldBeRight(username2 =>
            {
                Assert.Equal(username1.GetHashCode(), username2.GetHashCode());
            });
        });
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        const string usernameValue = "testuser";
        var result = Username.Create(usernameValue);

        // Act & Assert
        result.ShouldBeRight(username =>
        {
            string convertedValue = username;
            Assert.Equal(usernameValue, convertedValue);
        });
    }

    [Fact]
    public void Create_WithUsernameAtMinLength_ShouldReturnSuccess()
    {
        // Arrange
        var username = new string('a', Username.MinLength);

        // Act
        var result = Username.Create(username);

        // Assert
        result.ShouldBeRight(u => Assert.Equal(username, u.Value));
    }

    [Fact]
    public void Create_WithUsernameAtMaxLength_ShouldReturnSuccess()
    {
        // Arrange
        var username = new string('a', Username.MaxLength);

        // Act
        var result = Username.Create(username);

        // Assert
        result.ShouldBeRight(u => Assert.Equal(username, u.Value));
    }

    [Fact]
    public void Create_WithUsernameJustOverMaxLength_ShouldReturnTooLongError()
    {
        // Arrange
        var username = new string('a', Username.MaxLength + 1);

        // Act
        var result = Username.Create(username);

        // Assert
        result.ShouldBeLeft(error => Assert.Equal(UsersDomainErrors.Username.TooLong, error));
    }

    [Fact]
    public void Create_WithUsernameJustUnderMinLength_ShouldReturnTooShortError()
    {
        // Arrange
        var username = new string('a', Username.MinLength - 1);

        // Act
        var result = Username.Create(username);

        // Assert
        result.ShouldBeLeft(error => Assert.Equal(UsersDomainErrors.Username.TooShort, error));
    }
}