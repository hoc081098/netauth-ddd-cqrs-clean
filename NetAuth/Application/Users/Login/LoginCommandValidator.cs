using FluentValidation;
using NetAuth.Application.Core.Extensions;

namespace NetAuth.Application.Users.Login;

// ReSharper disable once UnusedType.Global
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
    }
}