using LanguageExt.UnitTesting;
using NetAuth.Domain.Users;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace NetAuth.UnitTests.Domain.Users;

public class PasswordTests
{
    [Theory]
    [InlineData("Pass1!")]
    [InlineData("Password123!")]
    [InlineData("MyP@ss1")]
    [InlineData("Secure#1")]
    [InlineData("Test@123")]
    [InlineData("ValidP@ssw0rd")]
    [InlineData("Str0ng!Pass")]
    [InlineData("C0mpl3x@Password")]
    [InlineData("P@ssw0rd!")]
    [InlineData("Qwerty123!")]
    public void Create_WithValidPassword_ShouldReturnSuccess(string validPassword)
    {
        // Act
        var result = Password.Create(validPassword);

        // Assert
        result.ShouldBeRight(right =>
        {
            Assert.Equal(validPassword, right.Value);
            Assert.Equal(validPassword, (string)right); // Test implicit conversion
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Create_WithNullOrWhiteSpace_ShouldReturnNullOrEmptyError(string? invalidPassword)
    {
        // Act
        var result = Password.Create(invalidPassword!);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Password.NullOrEmpty, left));
    }

    [Theory]
    [InlineData("A1!")]
    [InlineData("Pass1")]
    [InlineData("12345")]
    [InlineData("a")]
    [InlineData("Ab1!")]
    public void Create_WithPasswordTooShort_ShouldReturnTooShortError(string shortPassword)
    {
        // Act
        var result = Password.Create(shortPassword);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Password.TooShort, left));
    }

    [Theory]
    [InlineData("password1!")]
    [InlineData("alllowercase123!")]
    [InlineData("no_uppercase_1!")]
    [InlineData("lowercase@123")]
    public void Create_WithoutUppercaseLetter_ShouldReturnMissingUppercaseLetterError(string password)
    {
        // Act
        var result = Password.Create(password);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Password.MissingUppercaseLetter, left));
    }

    [Theory]
    [InlineData("PASSWORD1!")]
    [InlineData("ALLUPPERCASE123!")]
    [InlineData("NO_LOWERCASE_1!")]
    [InlineData("UPPERCASE@123")]
    public void Create_WithoutLowercaseLetter_ShouldReturnMissingLowercaseLetterError(string password)
    {
        // Act
        var result = Password.Create(password);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Password.MissingLowercaseLetter, left));
    }

    [Theory]
    [InlineData("Password!")]
    [InlineData("NoDigits@")]
    [InlineData("OnlyLetters!")]
    [InlineData("Test@Pass")]
    public void Create_WithoutDigit_ShouldReturnMissingDigitError(string password)
    {
        // Act
        var result = Password.Create(password);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Password.MissingDigit, left));
    }

    [Theory]
    [InlineData("Password1")]
    [InlineData("NoSpecialChar123")]
    [InlineData("OnlyLettersAndDigits1")]
    [InlineData("TestPass123")]
    [InlineData("Abc123def")]
    public void Create_WithoutNonAlphaNumeric_ShouldReturnMissingNonAlphaNumericError(string password)
    {
        // Act
        var result = Password.Create(password);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Password.MissingNonAlphaNumeric, left));
    }

    [Fact]
    public void Equals_WithSamePasswordValue_ShouldReturnTrue()
    {
        // Arrange
        var password1Result = Password.Create("Test@123");
        var password2Result = Password.Create("Test@123");

        // Act & Assert
        password1Result.ShouldBeRight(password1 =>
        {
            password2Result.ShouldBeRight(password2 =>
            {
                Assert.Equal(password1, password2);
                Assert.True(password1.Equals(password2));
                Assert.True(password1.Equals((object?)password2));
                Assert.True(password1 == password2);
                Assert.False(password1 != password2);
            });
        });
    }

    [Fact]
    public void Equals_WithDifferentPasswordValue_ShouldReturnFalse()
    {
        // Arrange
        var password1Result = Password.Create("Pass1!");
        var password2Result = Password.Create("Pass2!");

        // Act & Assert
        password1Result.ShouldBeRight(password1 =>
        {
            password2Result.ShouldBeRight(password2 =>
            {
                Assert.NotEqual(password1, password2);
                Assert.False(password1.Equals(password2));
                Assert.False(password1.Equals((object?)password2));
                Assert.False(password1 == password2);
                Assert.True(password1 != password2);
            });
        });
    }

    [Fact]
    public void GetHashCode_WithSamePasswordValue_ShouldReturnSameHashCode()
    {
        // Arrange
        var password1Result = Password.Create("Test@123");
        var password2Result = Password.Create("Test@123");

        // Act & Assert
        password1Result.ShouldBeRight(password1 =>
        {
            password2Result.ShouldBeRight(password2 =>
            {
                Assert.Equal(password1.GetHashCode(), password2.GetHashCode());
            });
        });
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        const string passwordValue = "Test@123";
        var result = Password.Create(passwordValue);

        // Act & Assert
        result.ShouldBeRight(password =>
        {
            string convertedValue = password;
            Assert.Equal(passwordValue, convertedValue);
        });
    }

    [Fact]
    public void Create_WithPasswordAtMinLength_ShouldReturnSuccess()
    {
        // Arrange - Min length is 6, need: 1 upper, 1 lower, 1 digit, 1 special
        const string password = "Aa1!bc"; // Exactly 6 characters

        // Act
        var result = Password.Create(password);

        // Assert
        result.ShouldBeRight(p => Assert.Equal(password, p.Value));
    }

    [Fact]
    public void Create_WithAllSpecialCharacters_ShouldReturnSuccess()
    {
        // Arrange - Test various special characters
        var passwords = new[]
        {
            "Pass1!",
            "Pass1@",
            "Pass1#",
            "Pass1$",
            "Pass1%",
            "Pass1^",
            "Pass1&",
            "Pass1*",
            "Pass1(",
            "Pass1)",
            "Pass1-",
            "Pass1_",
            "Pass1=",
            "Pass1+",
            "Pass1[",
            "Pass1]",
            "Pass1{",
            "Pass1}",
            "Pass1|",
            "Pass1\\",
            "Pass1:",
            "Pass1;",
            "Pass1\"",
            "Pass1'",
            "Pass1<",
            "Pass1>",
            "Pass1,",
            "Pass1.",
            "Pass1?",
            "Pass1/",
            "Pass1~",
            "Pass1`"
        };

        // Act & Assert
        foreach (var password in passwords)
        {
            var result = Password.Create(password);
            result.ShouldBeRight(p => Assert.Equal(password, p.Value));
        }
    }

    [Fact]
    public void Create_WithComplexPassword_ShouldReturnSuccess()
    {
        // Arrange
        const string complexPassword = "MyC0mpl3x!P@ssw0rd#2024$Secur3";

        // Act
        var result = Password.Create(complexPassword);

        // Assert
        result.ShouldBeRight(p => Assert.Equal(complexPassword, p.Value));
    }

    [Theory]
    [InlineData("Pass1")]  // Missing non-alphanumeric
    [InlineData("Pass!")]  // Missing digit
    [InlineData("pass1!")] // Missing uppercase
    [InlineData("PASS1!")] // Missing lowercase
    [InlineData("P1!")]    // Too short
    public void Create_WithMultipleValidationFailures_ShouldReturnFirstError(string password)
    {
        // Act
        var result = Password.Create(password);

        // Assert - Should return the first validation error encountered
        result.ShouldBeLeft(error =>
        {
            Assert.NotNull(error);
            Assert.NotEmpty(error.Code);
        });
    }
}