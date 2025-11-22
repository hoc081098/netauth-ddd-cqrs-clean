using FluentValidation;
using LanguageExt;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Application.Core.Extensions;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Users.Login;

public sealed record LoginCommand(
    string Email,
    string Password
) : ICommand<Either<DomainError, LoginResult>>;

public sealed record LoginResult(string AccessToken);

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