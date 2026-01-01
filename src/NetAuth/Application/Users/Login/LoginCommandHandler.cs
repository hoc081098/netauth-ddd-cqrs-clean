using System.Diagnostics.CodeAnalysis;
using LanguageExt;
using static LanguageExt.Prelude;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users;

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
            .MapAsync(async email => Optional(await userRepository.GetByEmailAsync(email, cancellationToken)))
            .BindAsync(userOption => AuthenticateUserAsync(command, userOption, cancellationToken));

    [SuppressMessage("Performance", "CA1849:Call async methods when in an async method")] // False positive
    private async Task<Either<DomainError, LoginResult>> AuthenticateUserAsync(
        LoginCommand command,
        Option<User> userOption,
        CancellationToken cancellationToken)
    {
        var user = userOption.IfNoneUnsafe(() => null);
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