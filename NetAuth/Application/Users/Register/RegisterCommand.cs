using System.Data;
using FluentValidation;
using LanguageExt;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Application.Core.Extensions;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users;

namespace NetAuth.Application.Users.Register;

public sealed record RegisterCommand(
    string Username,
    string Email,
    string Password
) : ICommand<Either<DomainError, RegisterResponse>>;

public sealed record RegisterResponse(string AccessToken);

internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty()
            .WithDomainError(UsersValidationErrors.Register.FirstNameIsRequired);

        RuleFor(command => command.Email)
            .NotEmpty()
            .WithDomainError(UsersValidationErrors.Register.EmailIsRequired);

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithDomainError(UsersValidationErrors.Register.PasswordIsRequired);
    }
}