using FluentValidation;
using JetBrains.Annotations;
using NetAuth.Application.Core.Extensions;

namespace NetAuth.Application.Users.LoginWithRefreshToken;

[UsedImplicitly]
internal sealed class LoginWithRefreshTokenCommandValidator : AbstractValidator<LoginWithRefreshTokenCommand>
{
    public LoginWithRefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithDomainError(UsersValidationErrors.LoginWithRefreshToken.RefreshTokenIsRequired);

        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .WithDomainError(UsersValidationErrors.LoginWithRefreshToken.DeviceIdIsRequired);

        RuleFor(x => x.DeviceId)
            .Must(FluentValidationExtensions.IsValidNonEmptyGuid)
            .WithDomainError(UsersValidationErrors.LoginWithRefreshToken.DeviceIdMustBeValidNonEmptyGuid);
    }
}