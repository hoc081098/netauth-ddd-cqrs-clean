using FluentValidation;
using JetBrains.Annotations;

namespace NetAuth.Application.Users.LoginWithRefreshToken;

[UsedImplicitly]
internal sealed class LoginWithRefreshTokenCommandValidator : AbstractValidator<LoginWithRefreshTokenCommand>
{
    public LoginWithRefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required.");
    }
}