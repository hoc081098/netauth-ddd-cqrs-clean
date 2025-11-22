using LanguageExt;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users;

namespace NetAuth.Application.Users.Login;

internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHashChecker passwordHashChecker,
    IJwtProvider jwtProvider) :
    ICommandHandler<LoginCommand, Either<DomainError, LoginResult>>
{
    public Task<Either<DomainError, LoginResult>> Handle(LoginCommand command,
        CancellationToken cancellationToken)
    {
        return Email.Create(command.Email)
            .MapAsync(email => userRepository.GetByEmailAsync(email, cancellationToken))
            .BindAsync(user => CheckPassword(command, user));
    }

    private Either<DomainError, LoginResult> CheckPassword(LoginCommand command, User? user)
    {
        if (user is null || !user.VerifyPasswordHash(command.Password, passwordHashChecker))
        {
            return UsersDomainErrors.User.InvalidCredentials;
        }

        var accessToken = jwtProvider.Create(user);
        return new LoginResult(AccessToken: accessToken);
    }
}