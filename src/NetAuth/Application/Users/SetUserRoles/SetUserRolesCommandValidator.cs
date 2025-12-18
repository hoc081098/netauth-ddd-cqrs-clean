using FluentValidation;
using JetBrains.Annotations;
using NetAuth.Application.Core.Extensions;

namespace NetAuth.Application.Users.SetUserRoles;

[UsedImplicitly]
internal sealed class SetUserRolesCommandValidator : AbstractValidator<SetUserRolesCommand>
{
    public SetUserRolesCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotEmpty()
            .WithDomainError(UsersValidationErrors.SetUserRoles.UserIdIsRequired);

        RuleFor(c => c.RoleIds)
            .NotEmpty()
            .WithDomainError(UsersValidationErrors.SetUserRoles.RoleIdsAreRequired);

        RuleFor(c => c.RoleChangeActor)
            .IsInEnum()
            .WithDomainError(UsersValidationErrors.SetUserRoles.RoleChangeActorIsInvalid);
    }
}