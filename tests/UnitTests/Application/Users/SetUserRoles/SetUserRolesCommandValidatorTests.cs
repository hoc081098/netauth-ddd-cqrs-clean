using FluentValidation.TestHelper;
using NetAuth.Application.Users;
using NetAuth.Application.Users.SetUserRoles;
using NetAuth.Domain.Users;

namespace NetAuth.UnitTests.Application.Users.SetUserRoles;

public class SetUserRolesCommandValidatorTests
{
    private readonly SetUserRolesCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenUserIdIsEmpty()
    {
        // Arrange
        var command = new SetUserRolesCommand(
            UserId: Guid.Empty,
            RoleIds: [1, 2],
            RoleChangeActor: RoleChangeActor.System);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(UsersValidationErrors.SetUserRoles.UserIdIsRequired.Message)
            .WithErrorCode(UsersValidationErrors.SetUserRoles.UserIdIsRequired.Code);
    }

    [Fact]
    public void ShouldHaveError_WhenRoleIdsIsEmpty()
    {
        // Arrange
        var command = new SetUserRolesCommand(
            UserId: Guid.NewGuid(),
            RoleIds: [],
            RoleChangeActor: RoleChangeActor.System);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleIds)
            .WithErrorMessage(UsersValidationErrors.SetUserRoles.RoleIdsAreRequired.Message)
            .WithErrorCode(UsersValidationErrors.SetUserRoles.RoleIdsAreRequired.Code);
    }

    [Fact]
    public void ShouldHaveError_WhenRoleIdsIsNull()
    {
        // Arrange
        var command = new SetUserRolesCommand(
            UserId: Guid.NewGuid(),
            RoleIds: null!,
            RoleChangeActor: RoleChangeActor.System);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleIds)
            .WithErrorMessage(UsersValidationErrors.SetUserRoles.RoleIdsAreRequired.Message)
            .WithErrorCode(UsersValidationErrors.SetUserRoles.RoleIdsAreRequired.Code);
    }

    [Fact]
    public void ShouldHaveError_WhenRoleChangeActorIsInvalid()
    {
        // Arrange
        var command = new SetUserRolesCommand(
            UserId: Guid.NewGuid(),
            RoleIds: [1, 2],
            RoleChangeActor: (RoleChangeActor)999); // Invalid enum value

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleChangeActor)
            .WithErrorMessage(UsersValidationErrors.SetUserRoles.RoleChangeActorIsInvalid.Message)
            .WithErrorCode(UsersValidationErrors.SetUserRoles.RoleChangeActorIsInvalid.Code);
    }

    [Fact]
    public void ShouldHaveMultipleErrors_WhenAllFieldsAreInvalid()
    {
        // Arrange
        var command = new SetUserRolesCommand(
            UserId: Guid.Empty,
            RoleIds: [],
            RoleChangeActor: (RoleChangeActor)999);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(UsersValidationErrors.SetUserRoles.UserIdIsRequired.Message);
        result.ShouldHaveValidationErrorFor(x => x.RoleIds)
            .WithErrorMessage(UsersValidationErrors.SetUserRoles.RoleIdsAreRequired.Message);
        result.ShouldHaveValidationErrorFor(x => x.RoleChangeActor)
            .WithErrorMessage(UsersValidationErrors.SetUserRoles.RoleChangeActorIsInvalid.Message);
    }

    [Fact]
    public void ShouldNotHaveError_WhenAllFieldsAreValid()
    {
        // Arrange
        var command = new SetUserRolesCommand(
            UserId: Guid.NewGuid(),
            RoleIds: [1, 2, 3],
            RoleChangeActor: RoleChangeActor.System);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        result.ShouldNotHaveValidationErrorFor(x => x.RoleIds);
        result.ShouldNotHaveValidationErrorFor(x => x.RoleChangeActor);
    }

    [Theory]
    [InlineData(RoleChangeActor.User)]
    [InlineData(RoleChangeActor.Privileged)]
    [InlineData(RoleChangeActor.System)]
    public void ShouldNotHaveError_WhenRoleChangeActorIsValid(RoleChangeActor roleChangeActor)
    {
        // Arrange
        var command = new SetUserRolesCommand(
            UserId: Guid.NewGuid(),
            RoleIds: [1, 2],
            RoleChangeActor: roleChangeActor);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RoleChangeActor);
    }

    [Fact]
    public void ShouldNotHaveError_WhenRoleIdsHasSingleRole()
    {
        // Arrange
        var command = new SetUserRolesCommand(
            UserId: Guid.NewGuid(),
            RoleIds: [1],
            RoleChangeActor: RoleChangeActor.System);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RoleIds);
    }

    [Fact]
    public void ShouldNotHaveError_WhenRoleIdsHasMultipleRoles()
    {
        // Arrange
        var command = new SetUserRolesCommand(
            UserId: Guid.NewGuid(),
            RoleIds: [1, 2, 3, 4, 5],
            RoleChangeActor: RoleChangeActor.System);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RoleIds);
    }
}

