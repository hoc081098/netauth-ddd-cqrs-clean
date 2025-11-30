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
        var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        // 1. Find the refresh tokenHash
        var refreshToken = await refreshTokenRepository.GetByTokenHashAsync(
            refreshTokenGenerator.ComputeTokenHash(command.RefreshToken),
            cancellationToken);

        // 2. Check if tokenHash exists
        if (refreshToken is null)
        {
            return UsersDomainErrors.RefreshToken.Invalid;
        }

        // 3. Check status: refresh token reuse detection
        if (refreshToken.Status != RefreshTokenStatus.Active)
        {
            // token đã bị rotate / revoked mà còn dùng lại → considered reused
            refreshToken.MarkAsCompromised(clock.UtcNow);
            await MarkRefreshTokenChainCompromised(refreshToken.UserId, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return UsersDomainErrors.RefreshToken.Revoked;
        }

        // 4. Check if tokenHash is expired
        if (refreshToken.IsExpired(clock.UtcNow))
        {
            refreshToken.MarkAsRevoked(clock.UtcNow);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return UsersDomainErrors.RefreshToken.Expired;
        }

        // 5. Check device ID
        if (!string.Equals(refreshToken.DeviceId, command.DeviceId, StringComparison.Ordinal))
        {
            // device ID không khớp → nghi ngờ bị đánh cắp token -> chặn luôn
            refreshToken.MarkAsCompromised(clock.UtcNow);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return UsersDomainErrors.RefreshToken.InvalidDevice;
        }

        // 5. Rotate the refresh token and generate a new access token
        var accessToken = jwtProvider.Create(refreshToken.User);
        var refreshTokenResult = refreshTokenGenerator.GenerateRefreshToken();

        var newRefreshToken = refreshToken.Rotate(
            newTokenHash: refreshTokenResult.TokenHash,
            newExpiresOnUtc: clock.UtcNow.Add(jwtConfigOptions.Value.RefreshTokenExpiration));
        refreshTokenRepository.Insert(newRefreshToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new LoginWithRefreshTokenResult(
            AccessToken: accessToken,
            RefreshToken: refreshTokenResult.RawToken);
    }

    private async Task MarkRefreshTokenChainCompromised(Guid userId, CancellationToken ct)
    {
        // đơn giản: revoke tất cả token active của user
        var refreshTokens = await refreshTokenRepository.GetActiveByUserIdAsync(userId, ct);
        var now = DateTimeOffset.UtcNow;

        foreach (var token in refreshTokens)
        {
            token.MarkAsCompromised(now);
        }
    }
}