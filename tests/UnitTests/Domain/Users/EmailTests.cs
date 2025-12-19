using LanguageExt.UnitTesting;
using NetAuth.Domain.Users;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace NetAuth.UnitTests.Domain.Users;

public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.com")]
    [InlineData("user_name@example.com")]
    [InlineData("user123@example.co.uk")]
    [InlineData("test@sub.example.com")]
    [InlineData("a@b.co")]
    [InlineData("TEST@EXAMPLE.COM")]
    [InlineData("Test.User@Example.Com")]
    public void Create_WithValidEmail_ShouldReturnSuccess(string validEmail)
    {
        // Act
        var result = Email.Create(validEmail);

        // Assert
        result.ShouldBeRight(right =>
        {
            Assert.Equal(validEmail, right.Value);
            Assert.Equal(validEmail, (string)right); // Test implicit conversion
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Create_WithNullOrWhiteSpace_ShouldReturnNullOrEmptyError(string? invalidEmail)
    {
        // Act
        var result = Email.Create(invalidEmail!);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Email.NullOrEmpty, left));
    }

    [Fact]
    public void Create_WithEmailExceedingMaxLength_ShouldReturnTooLongError()
    {
        // Arrange
        var longEmail = new string('a', Email.MaxLength) + "@example.com"; // Exceeds MaxLength

        // Act
        var result = Email.Create(longEmail);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Email.TooLong, left));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    [InlineData("invalid@.com")]
    [InlineData("invalid@domain")]
    [InlineData("invalid.domain.com")]
    [InlineData("invalid.-domain.com")]
    [InlineData("invalid.-domain-.com")]
    [InlineData("invalid.domain-.com")]
    [InlineData("invalid@domain..com")]
    [InlineData("invalid@@domain.com")]
    [InlineData("invalid @domain.com")]
    [InlineData("invalid@domain .com")]
    [InlineData("invalid@domain.c")]
    [InlineData("user name@example.com")]
    public void Create_WithInvalidFormat_ShouldReturnInvalidFormatError(string invalidEmail)
    {
        // Act
        var result = Email.Create(invalidEmail);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Email.InvalidFormat, left));
    }

    [Fact]
    public void Equals_WithSameEmailValue_ShouldReturnTrue()
    {
        // Arrange
        var email1Result = Email.Create("test@example.com");
        var email2Result = Email.Create("test@example.com");

        // Act & Assert
        email1Result.ShouldBeRight(email1 =>
        {
            email2Result.ShouldBeRight(email2 =>
            {
                Assert.Equal(email1, email2);
                Assert.True(email1.Equals(email2));
                Assert.True(email1.Equals((object?)email2));
                Assert.True(email1 == email2);
                Assert.False(email1 != email2);
            });
        });
    }

    [Fact]
    public void Equals_WithDifferentEmailValue_ShouldReturnFalse()
    {
        // Arrange
        var email1Result = Email.Create("test1@example.com");
        var email2Result = Email.Create("test2@example.com");

        // Act & Assert
        email1Result.ShouldBeRight(email1 =>
        {
            email2Result.ShouldBeRight(email2 =>
            {
                Assert.NotEqual(email1, email2);
                Assert.False(email1.Equals(email2));
                Assert.False(email1.Equals((object?)email2));
                Assert.False(email1 == email2);
                Assert.True(email1 != email2);
            });
        });
    }

    [Fact]
    public void GetHashCode_WithSameEmailValue_ShouldReturnSameHashCode()
    {
        // Arrange
        var email1Result = Email.Create("test@example.com");
        var email2Result = Email.Create("test@example.com");

        // Act & Assert
        email1Result.ShouldBeRight(email1 =>
        {
            email2Result.ShouldBeRight(email2 => { Assert.Equal(email1.GetHashCode(), email2.GetHashCode()); });
        });
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        const string emailValue = "test@example.com";
        var result = Email.Create(emailValue);

        // Act & Assert
        result.ShouldBeRight(email =>
        {
            string convertedValue = email;
            Assert.Equal(emailValue, convertedValue);
        });
    }

    [Fact]
    public void Create_WithEmailAtMaxLength_ShouldReturnSuccess()
    {
        // Arrange
        // Create email exactly at max length: "a...a@b.co" = 256 chars
        var localPart = new string('a', Email.MaxLength - 5); // -5 for "@b.co"
        var email = $"{localPart}@b.co";

        // Act
        var result = Email.Create(email);

        // Assert
        result.ShouldBeRight(e => Assert.Equal(email, e.Value));
    }

    [Fact]
    public void Create_WithEmailJustOverMaxLength_ShouldReturnTooLongError()
    {
        // Arrange
        // Create email just over max length: 257 chars
        var localPart = new string('a', Email.MaxLength - 4); // -4 for "@b.co" = 257 total
        var email = $"{localPart}@b.co";

        // Act
        var result = Email.Create(email);

        // Assert
        result.ShouldBeLeft(error => Assert.Equal(UsersDomainErrors.Email.TooLong, error));
    }
}