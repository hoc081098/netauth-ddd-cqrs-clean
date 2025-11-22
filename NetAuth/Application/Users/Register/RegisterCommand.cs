using FluentValidation;
using LanguageExt;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Application.Core.Extensions;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Users.Register;

public sealed record RegisterCommand(
    string Username,
    string Email,
    string Password
) : ICommand<Either<DomainError, RegisterResult>>;

public sealed record RegisterResult(string AccessToken);

// ReSharper disable once UnusedType.Global
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