using FluentValidation.TestHelper;
using NetAuth.Application.Users;
using NetAuth.Application.Users.LoginWithRefreshToken;

namespace NetAuth.UnitTests.Application.Users.LoginWithRefreshToken;

public class LoginWithRefreshTokenCommandValidatorTests
{
    private readonly LoginWithRefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_RefreshToken_Is_Empty()
    {
        // Arrange
        var command = new LoginWithRefreshTokenCommand(
            RefreshToken: string.Empty,
            DeviceId: Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
            .WithErrorMessage(UsersValidationErrors.LoginWithRefreshToken.RefreshTokenIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.LoginWithRefreshToken.RefreshTokenIsRequired.Code);
    }

    [Fact]
    public void Should_Have_Error_When_RefreshToken_Is_Null()
    {
        // Arrange
        var command = new LoginWithRefreshTokenCommand(
            RefreshToken: null!,
            DeviceId: Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
            .WithErrorMessage(UsersValidationErrors.LoginWithRefreshToken.RefreshTokenIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.LoginWithRefreshToken.RefreshTokenIsRequired.Code);
    }

    [Fact]
    public void Should_Have_Error_When_DeviceId_Is_Empty()
    {
        // Arrange
        var command = new LoginWithRefreshTokenCommand(
            RefreshToken: "valid-refresh-token",
            DeviceId: Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DeviceId)
            .WithErrorMessage(UsersValidationErrors.LoginWithRefreshToken.DeviceIdIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.LoginWithRefreshToken.DeviceIdIsRequired.Code);
    }

    [Fact]
    public void Should_Have_Multiple_Errors_When_All_Fields_Are_Invalid()
    {
        // Arrange
        var command = new LoginWithRefreshTokenCommand(
            RefreshToken: string.Empty,
            DeviceId: Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
            .WithErrorMessage(UsersValidationErrors.LoginWithRefreshToken.RefreshTokenIsRequired.Message);
        result.ShouldHaveValidationErrorFor(x => x.DeviceId)
            .WithErrorMessage(UsersValidationErrors.LoginWithRefreshToken.DeviceIdIsRequired.Message);
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Are_Valid()
    {
        // Arrange
        var command = new LoginWithRefreshTokenCommand(
            RefreshToken: "valid-refresh-token",
            DeviceId: Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RefreshToken);
        result.ShouldNotHaveValidationErrorFor(x => x.DeviceId);
    }

    [Theory]
    [InlineData("   ")] // Whitespace
    [InlineData("\t")]  // Tab
    [InlineData("\n")]  // Newline
    public void Should_Have_Error_When_RefreshToken_Is_Whitespace(string refreshToken)
    {
        // Arrange
        var command = new LoginWithRefreshTokenCommand(
            RefreshToken: refreshToken,
            DeviceId: Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
            .WithErrorMessage(UsersValidationErrors.LoginWithRefreshToken.RefreshTokenIsRequired.Message);
    }
}

