using FluentValidation.TestHelper;
using NetAuth.Application.Users;
using NetAuth.Application.Users.Register;

namespace NetAuth.UnitTests.Application.Users.Register;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Username_Is_Empty()
    {
        // Arrange
        var command = new RegisterCommand(
            Username: string.Empty,
            Email: "test@example.com",
            Password: "ValidPassword123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage(UsersValidationErrors.Register.UsernameIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.Register.UsernameIsRequired.Code);
    }

    [Fact]
    public void Should_Have_Error_When_Username_Is_Null()
    {
        // Arrange
        var command = new RegisterCommand(
            Username: null!,
            Email: "test@example.com",
            Password: "ValidPassword123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage(UsersValidationErrors.Register.UsernameIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.Register.UsernameIsRequired.Code);
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Empty()
    {
        // Arrange
        var command = new RegisterCommand(
            Username: "testuser",
            Email: string.Empty,
            Password: "ValidPassword123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage(UsersValidationErrors.Register.EmailIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.Register.EmailIsRequired.Code);
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Null()
    {
        // Arrange
        var command = new RegisterCommand(
            Username: "testuser",
            Email: null!,
            Password: "ValidPassword123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage(UsersValidationErrors.Register.EmailIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.Register.EmailIsRequired.Code);
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Empty()
    {
        // Arrange
        var command = new RegisterCommand(
            Username: "testuser",
            Email: "test@example.com",
            Password: string.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage(UsersValidationErrors.Register.PasswordIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.Register.PasswordIsRequired.Code);
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Null()
    {
        // Arrange
        var command = new RegisterCommand(
            Username: "testuser",
            Email: "test@example.com",
            Password: null!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage(UsersValidationErrors.Register.PasswordIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.Register.PasswordIsRequired.Code);
    }

    [Fact]
    public void Should_Have_Multiple_Errors_When_All_Fields_Are_Empty()
    {
        // Arrange
        var command = new RegisterCommand(
            Username: string.Empty,
            Email: string.Empty,
            Password: string.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage(UsersValidationErrors.Register.UsernameIsRequired.Message);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage(UsersValidationErrors.Register.EmailIsRequired.Message);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage(UsersValidationErrors.Register.PasswordIsRequired.Message);
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Are_Valid()
    {
        // Arrange
        var command = new RegisterCommand(
            Username: "testuser",
            Email: "test@example.com",
            Password: "ValidPassword123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("   ")] // Whitespace
    [InlineData("\t")]  // Tab
    [InlineData("\n")]  // Newline
    public void Should_Have_Error_When_Username_Is_Whitespace(string username)
    {
        // Arrange
        var command = new RegisterCommand(
            Username: username,
            Email: "test@example.com",
            Password: "ValidPassword123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage(UsersValidationErrors.Register.UsernameIsRequired.Message);
    }

    [Theory]
    [InlineData("   ")] // Whitespace
    [InlineData("\t")]  // Tab
    [InlineData("\n")]  // Newline
    public void Should_Have_Error_When_Email_Is_Whitespace(string email)
    {
        // Arrange
        var command = new RegisterCommand(
            Username: "testuser",
            Email: email,
            Password: "ValidPassword123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage(UsersValidationErrors.Register.EmailIsRequired.Message);
    }

    [Theory]
    [InlineData("   ")] // Whitespace
    [InlineData("\t")]  // Tab
    [InlineData("\n")]  // Newline
    public void Should_Have_Error_When_Password_Is_Whitespace(string password)
    {
        // Arrange
        var command = new RegisterCommand(
            Username: "testuser",
            Email: "test@example.com",
            Password: password);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage(UsersValidationErrors.Register.PasswordIsRequired.Message);
    }
}

