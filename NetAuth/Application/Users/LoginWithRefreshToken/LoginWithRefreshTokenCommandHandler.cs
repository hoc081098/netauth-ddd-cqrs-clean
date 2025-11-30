using LanguageExt;
using Microsoft.Extensions.Options;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Abstractions.Messaging;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users;
using NetAuth.Infrastructure.Authentication;

namespace NetAuth.Application.Users.LoginWithRefreshToken;

internal sealed class LoginWithRefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IJwtProvider jwtProvider,
    IRefreshTokenGenerator refreshTokenGenerator,
    IOptions<JwtConfig> jwtConfigOptions,
    IClock clock,
    IUnitOfWork unitOfWork) :
    ICommandHandler<LoginWithRefreshTokenCommand, LoginWithRefreshTokenResult>
{
    public async Task<Either<DomainError, LoginWithRefreshTokenResult>> Handle(
        LoginWithRefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Find the refresh token
        var storedRefreshToken = await refreshTokenRepository.GetByTokenAsync(
            command.RefreshToken,
            cancellationToken);

        // 2. Check if token exists
        if (storedRefreshToken is null)
        {
            return UsersDomainErrors.RefreshToken.Invalid;
        }

        // 3. Check if token is expired
        if (storedRefreshToken.IsExpired(clock.UtcNow))
        {
            return UsersDomainErrors.RefreshToken.Expired;
        }

        // 4. Generate new access token and new refresh token
        var accessToken = jwtProvider.Create(storedRefreshToken.User);
        storedRefreshToken.UpdateToken(
            token: refreshTokenGenerator.GenerateToken(),
            expiresOnUtc: clock.UtcNow.Add(jwtConfigOptions.Value.RefreshTokenExpiration));
        await refreshTokenRepository.DeleteExpiredByUserIdAsync(storedRefreshToken.UserId, clock.UtcNow,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginWithRefreshTokenResult(
            AccessToken: accessToken,
            RefreshToken: storedRefreshToken.Token);
    }
}