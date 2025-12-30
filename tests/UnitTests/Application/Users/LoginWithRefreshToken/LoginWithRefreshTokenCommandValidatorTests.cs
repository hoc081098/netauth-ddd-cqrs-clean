using FluentValidation.TestHelper;
using NetAuth.Application.Users;
using NetAuth.Application.Users.LoginWithRefreshToken;

namespace NetAuth.UnitTests.Application.Users.LoginWithRefreshToken;

public class LoginWithRefreshTokenCommandValidatorTests
{
    private readonly LoginWithRefreshTokenCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenRefreshTokenIsEmpty()
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
    public void ShouldHaveError_WhenRefreshTokenIsNull()
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
    public void ShouldHaveError_WhenDeviceIdIsEmpty()
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
    public void ShouldHaveMultipleErrors_WhenAllFieldsAreInvalid()
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
    public void ShouldNotHaveError_WhenAllFieldsAreValid()
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
    public void ShouldHaveError_WhenRefreshTokenIsWhitespace(string refreshToken)
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

