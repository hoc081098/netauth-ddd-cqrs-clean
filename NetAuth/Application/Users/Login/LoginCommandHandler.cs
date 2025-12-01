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

        // Generate refresh token and save it.
        var refreshTokenResult = refreshTokenGenerator.GenerateRefreshToken();
        var refreshToken = RefreshToken.Create(
            tokenHash: refreshTokenResult.TokenHash,
            expiresOnUtc: clock.UtcNow.Add(refreshTokenGenerator.RefreshTokenExpiration),
            userId: user.Id,
            deviceId: command.DeviceId
        );
        refreshTokenRepository.Insert(refreshToken);

        // Save changes
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResult(
            AccessToken: accessToken,
            RefreshToken: refreshTokenResult.RawToken);
    }
}