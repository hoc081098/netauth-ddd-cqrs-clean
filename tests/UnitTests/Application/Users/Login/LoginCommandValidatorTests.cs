using FluentValidation.TestHelper;
using NetAuth.Application.Users;
using NetAuth.Application.Users.Login;

namespace NetAuth.UnitTests.Application.Users.Login;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Email_Is_Empty()
    {
        // Arrange
        var command = new LoginCommand(
            Email: string.Empty,
            Password: "ValidPassword123",
            DeviceId: Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage(UsersValidationErrors.Login.EmailIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.Login.EmailIsRequired.Code);
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Null()
    {
        // Arrange
        var command = new LoginCommand(
            Email: null!,
            Password: "ValidPassword123",
            DeviceId: Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage(UsersValidationErrors.Login.EmailIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.Login.EmailIsRequired.Code);
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Empty()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "test@example.com",
            Password: string.Empty,
            DeviceId: Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage(UsersValidationErrors.Login.PasswordIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.Login.PasswordIsRequired.Code);
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Null()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "test@example.com",
            Password: null!,
            DeviceId: Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage(UsersValidationErrors.Login.PasswordIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.Login.PasswordIsRequired.Code);
    }

    [Fact]
    public void Should_Have_Error_When_DeviceId_Is_Empty()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "test@example.com",
            Password: "ValidPassword123",
            DeviceId: Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DeviceId)
            .WithErrorMessage(UsersValidationErrors.Login.DeviceIdIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.Login.DeviceIdIsRequired.Code);
    }

    [Fact]
    public void Should_Have_Multiple_Errors_When_All_Fields_Are_Invalid()
    {
        // Arrange
        var command = new LoginCommand(
            Email: string.Empty,
            Password: string.Empty,
            DeviceId: Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage(UsersValidationErrors.Login.EmailIsRequired.Message);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage(UsersValidationErrors.Login.PasswordIsRequired.Message);
        result.ShouldHaveValidationErrorFor(x => x.DeviceId)
            .WithErrorMessage(UsersValidationErrors.Login.DeviceIdIsRequired.Message);
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Are_Valid()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "test@example.com",
            Password: "ValidPassword123",
            DeviceId: Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
        result.ShouldNotHaveValidationErrorFor(x => x.DeviceId);
    }

    [Theory]
    [InlineData("   ")] // Whitespace
    [InlineData("\t")]  // Tab
    [InlineData("\n")]  // Newline
    public void Should_Have_Error_When_Email_Is_Whitespace(string email)
    {
        // Arrange
        var command = new LoginCommand(
            Email: email,
            Password: "ValidPassword123",
            DeviceId: Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage(UsersValidationErrors.Login.EmailIsRequired.Message);
    }

    [Theory]
    [InlineData("   ")] // Whitespace
    [InlineData("\t")]  // Tab
    [InlineData("\n")]  // Newline
    public void Should_Have_Error_When_Password_Is_Whitespace(string password)
    {
        // Arrange
        var command = new LoginCommand(
            Email: "test@example.com",
            Password: password,
            DeviceId: Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage(UsersValidationErrors.Login.PasswordIsRequired.Message);
    }
}

