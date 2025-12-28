using FluentValidation;
using JetBrains.Annotations;
using NetAuth.Application.Core.Extensions;

namespace NetAuth.Application.Users.Login;

[UsedImplicitly]
internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(query => query.Email)
            .NotEmpty()
            .WithDomainError(UsersValidationErrors.Login.EmailIsRequired);

        RuleFor(query => query.Password)
            .NotEmpty()
            .WithDomainError(UsersValidationErrors.Login.PasswordIsRequired);

        RuleFor(query => query.DeviceId)
            .NotEmpty()
            .WithDomainError(UsersValidationErrors.Login.DeviceIdIsRequired);
    }
}