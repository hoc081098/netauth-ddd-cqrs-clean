using LanguageExt.UnitTesting;
using Microsoft.EntityFrameworkCore.Storage;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Users.LoginWithRefreshToken;
using NetAuth.Domain.Users;
using NetAuth.Domain.Users.DomainEvents;
using NetAuth.UnitTests.Application.Abstractions.Common;
using NetAuth.UnitTests.Domain.Users;
using NSubstitute;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace NetAuth.UnitTests.Application.Users.LoginWithRefreshToken;

public class LoginWithRefreshTokenCommandHandlerTests
{
    // Fixed point in time for consistent test results
    private static readonly DateTimeOffset Now = DateTimeOffset.Now;

    // Subject under test (SUT)
    private readonly LoginWithRefreshTokenCommandHandler _handler;

    // Dependencies
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDbContextTransaction _transaction;

    public LoginWithRefreshTokenCommandHandlerTests()
    {
        // Set up
        _refreshTokenGenerator = Substitute.For<IRefreshTokenGenerator>();
        _jwtProvider = Substitute.For<IJwtProvider>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _clock = FixedClock.Create(Now);
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _transaction = Substitute.For<IDbContextTransaction>();

        _handler = new LoginWithRefreshTokenCommandHandler(
            _refreshTokenRepository,
            _jwtProvider,
            _refreshTokenGenerator,
            _clock,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenDoesNotExist_ShouldReturnDomainError()
    {
        // Arrange
        var refreshToken = "some-refresh-token";
        var deviceId = "device-123";
        var hashedRefreshToken = "hashed-refresh-token";

        var command = new LoginWithRefreshTokenCommand(
            RefreshToken: refreshToken,
            DeviceId: deviceId
        );

        _refreshTokenGenerator
            .ComputeTokenHash(refreshToken)
            .Returns(hashedRefreshToken);

        _refreshTokenRepository
            .GetByTokenHashAsync(hashedRefreshToken, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RefreshToken?>(null));

        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_transaction);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.RefreshToken.Invalid, left));

        await _unitOfWork.Received(1)
            .BeginTransactionAsync(Arg.Any<CancellationToken>());

        await _refreshTokenRepository.Received(1)
            .GetByTokenHashAsync(hashedRefreshToken, Arg.Any<CancellationToken>());

        await _transaction.DidNotReceiveWithAnyArgs()
            .CommitAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenStatusIsNotActive_ShouldReturnDomainError()
    {
        // Arrange
        var rawRefreshToken = "some-refresh-token";
        var deviceId = "device-123";

        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        refreshToken.MarkAsRevokedDueToExpiration(Now);

        // Create additional active tokens that should be marked as compromised
        var activeToken1 = RefreshTokenTestData.CreateRefreshToken(userId: refreshToken.UserId);
        var activeToken2 = RefreshTokenTestData.CreateRefreshToken(userId: refreshToken.UserId);
        var activeTokensInChain = new List<RefreshToken> { activeToken1, activeToken2 };

        var command = new LoginWithRefreshTokenCommand(
            RefreshToken: rawRefreshToken,
            DeviceId: deviceId);

        _refreshTokenGenerator
            .ComputeTokenHash(rawToken: rawRefreshToken)
            .Returns(refreshToken.TokenHash);

        _refreshTokenRepository
            .GetByTokenHashAsync(refreshToken.TokenHash, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RefreshToken?>(refreshToken));

        _refreshTokenRepository
            .GetNonExpiredActiveTokensByUserIdAsync(refreshToken.UserId, Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<RefreshToken>>(activeTokensInChain));

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_transaction);

        _transaction.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.RefreshToken.Revoked, left));

        await _unitOfWork.Received(1)
            .BeginTransactionAsync(Arg.Any<CancellationToken>());

        await _refreshTokenRepository.Received(1)
            .GetByTokenHashAsync(refreshToken.TokenHash, Arg.Any<CancellationToken>());

        await _refreshTokenRepository.Received(1)
            .GetNonExpiredActiveTokensByUserIdAsync(refreshToken.UserId, Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());

        await _transaction.Received(1)
            .CommitAsync(Arg.Any<CancellationToken>());

        // Assert the reused token is marked as compromised
        Assert.Equal(RefreshTokenStatus.Compromised, refreshToken.Status);
        Assert.Equal(Now, refreshToken.RevokedAt);
        Assert.Single(refreshToken.DomainEvents.OfType<RefreshTokenReuseDetectedDomainEvent>());
        Assert.Single(refreshToken.DomainEvents.OfType<RefreshTokenChainCompromisedDomainEvent>());

        // Assert all tokens in the chain are also marked as compromised
        Assert.All(activeTokensInChain, static token =>
        {
            Assert.Equal(RefreshTokenStatus.Compromised, token.Status);
            Assert.Equal(Now, token.RevokedAt);
            Assert.Single(token.DomainEvents.OfType<RefreshTokenReuseDetectedDomainEvent>());
            Assert.Empty(token.DomainEvents.OfType<RefreshTokenChainCompromisedDomainEvent>());
        });
    }
}