using LanguageExt;
using Microsoft.Extensions.Options;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users;
using NetAuth.Infrastructure.Authentication;

namespace NetAuth.Application.Users.Login;

internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHashChecker passwordHashChecker,
    IJwtProvider jwtProvider,
    IRefreshTokenGenerator refreshTokenGenerator,
    IOptions<JwtConfig> jwtConfigOptions,
    IClock clock,
    IUnitOfWork unitOfWork) :
    ICommandHandler<LoginCommand, LoginResult>
{
    public Task<Either<DomainError, LoginResult>> Handle(LoginCommand command,
        CancellationToken cancellationToken) =>
        Email.Create(command.Email)
            .MapAsync(email => userRepository.GetByEmailAsync(email, cancellationToken))
            .BindAsync(user => AuthenticateUserAsync(command, user, cancellationToken));

    private async Task<Either<DomainError, LoginResult>> AuthenticateUserAsync(
        LoginCommand command,
        User? user,
        CancellationToken cancellationToken)
    {
        if (user is null || !user.VerifyPasswordHash(command.Password, passwordHashChecker))
        {
            return UsersDomainErrors.User.InvalidCredentials;
        }

        // Generate access token
        var accessToken = jwtProvider.Create(user);

        // Generate refresh token, clean up old expired tokens, and save new one
        var refreshToken = RefreshToken.Create(
            token: refreshTokenGenerator.GenerateToken(),
            expiresOnUtc: clock.UtcNow.Add(jwtConfigOptions.Value.RefreshTokenExpiration),
            userId: user.Id);
        await refreshTokenRepository.DeleteExpiredByUserIdAsync(user.Id, clock.UtcNow, cancellationToken);
        refreshTokenRepository.Insert(refreshToken);

        // Save changes
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResult(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token);
    }
}