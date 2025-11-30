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
        var utcNow = clock.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        // 1. Find the refresh token
        var refreshToken = await refreshTokenRepository.GetByTokenHashAsync(
            refreshTokenGenerator.ComputeTokenHash(command.RefreshToken),
            cancellationToken);

        // 2. Check if token exists
        if (refreshToken is null)
        {
            return UsersDomainErrors.RefreshToken.Invalid;
        }

        // 3. Check status: refresh token reuse detection
        if (refreshToken.Status != RefreshTokenStatus.Active)
        {
            // token đã bị rotate / revoked mà còn dùng lại → considered reused
            refreshToken.MarkAsCompromised(utcNow);
            await MarkRefreshTokenChainCompromised(
                refreshTokenRepository,
                refreshToken.UserId,
                utcNow,
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return UsersDomainErrors.RefreshToken.Revoked;
        }

        // 4. Check if tokenHash is expired
        if (refreshToken.IsExpired(utcNow))
        {
            refreshToken.MarkAsRevoked(utcNow);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return UsersDomainErrors.RefreshToken.Expired;
        }

        // 5. Check device ID
        if (!string.Equals(refreshToken.DeviceId, command.DeviceId, StringComparison.Ordinal))
        {
            // device ID không khớp → nghi ngờ bị đánh cắp token -> chặn luôn
            refreshToken.MarkAsCompromised(utcNow);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return UsersDomainErrors.RefreshToken.InvalidDevice;
        }

        // 6. Final validation
        if (!refreshToken.IsValid(utcNow, command.DeviceId))
        {
            // Double check, should not reach here
            return UsersDomainErrors.RefreshToken.Invalid;
        }

        // 6. Rotate the refresh token and generate a new access token
        var accessToken = jwtProvider.Create(refreshToken.User);
        var refreshTokenResult = refreshTokenGenerator.GenerateRefreshToken();

        var newRefreshToken = refreshToken.Rotate(
            newTokenHash: refreshTokenResult.TokenHash,
            newExpiresOnUtc: utcNow.Add(jwtConfigOptions.Value.RefreshTokenExpiration),
            revokedAt: utcNow);
        refreshTokenRepository.Insert(newRefreshToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new LoginWithRefreshTokenResult(
            AccessToken: accessToken,
            RefreshToken: refreshTokenResult.RawToken);
    }

    private static async Task MarkRefreshTokenChainCompromised(
        IRefreshTokenRepository refreshTokenRepository,
        Guid userId,
        DateTimeOffset currentUtc,
        CancellationToken cancellationToken = default)
    {
        // Simple approach: revoke all active tokens for the user
        var refreshTokens = await refreshTokenRepository.GetValidTokensByUserIdAsync(userId,
            currentUtc,
            cancellationToken);

        foreach (var token in refreshTokens)
        {
            token.MarkAsCompromised(currentUtc);
        }
    }
}