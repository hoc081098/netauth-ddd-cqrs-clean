using FluentValidation;
using JetBrains.Annotations;
using NetAuth.Application.Core.Extensions;

namespace NetAuth.Application.Users.Register;

[UsedImplicitly]
internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty()
            .WithDomainError(UsersValidationErrors.Register.UsernameIsRequired);

        RuleFor(command => command.Email)
            .NotEmpty()
            .WithDomainError(UsersValidationErrors.Register.EmailIsRequired);

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithDomainError(UsersValidationErrors.Register.PasswordIsRequired);
    }
}