using LanguageExt.UnitTesting;
using Microsoft.EntityFrameworkCore.Storage;
using NetAuth.Application.Abstractions.Authentication;
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

    // Test data constants
    private const string RefreshTokenRaw = "some-refresh-token";
    private const string HashedRefreshToken = "hashed-refresh-token";
    private static readonly Guid DeviceId = Guid.NewGuid();

    private static readonly LoginWithRefreshTokenCommand Command = new(
        RefreshToken: RefreshTokenRaw,
        DeviceId: DeviceId);

    // Subject under test (SUT)
    private readonly LoginWithRefreshTokenCommandHandler _handler;

    // Dependencies (mocks)
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDbContextTransaction _transaction;

    public LoginWithRefreshTokenCommandHandlerTests()
    {
        // Initialize mocks
        _refreshTokenGenerator = Substitute.For<IRefreshTokenGenerator>();
        _jwtProvider = Substitute.For<IJwtProvider>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _transaction = Substitute.For<IDbContextTransaction>();

        var clock = FixedClock.Create(Now);

        _handler = new LoginWithRefreshTokenCommandHandler(
            _refreshTokenRepository,
            _jwtProvider,
            _refreshTokenGenerator,
            clock,
            _unitOfWork);

        // Setup common mocks
        SetupCommonMocks();
    }

    private void SetupCommonMocks()
    {
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_transaction);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _transaction.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    private void SetupTokenHashComputation(string tokenHash)
    {
        _refreshTokenGenerator
            .ComputeTokenHash(rawToken: Command.RefreshToken)
            .Returns(tokenHash);
    }

    private void SetupGetRefreshToken(RefreshToken? refreshToken)
    {
        var tokenHash = refreshToken?.TokenHash ?? HashedRefreshToken;
        SetupTokenHashComputation(tokenHash);

        _refreshTokenRepository
            .GetByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(refreshToken));
    }

    private static void AssertTransactionFlow(
        IUnitOfWork unitOfWork,
        IDbContextTransaction transaction,
        bool shouldCommit = true)
    {
        unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());

        if (shouldCommit)
        {
            unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
            transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        }
        else
        {
            unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
            transaction.DidNotReceiveWithAnyArgs().CommitAsync(CancellationToken.None);
        }
    }

    private static void AssertTokenMarkedAsCompromised(
        RefreshToken refreshToken,
        Type expectedDomainEventType)
    {
        Assert.Equal(RefreshTokenStatus.Compromised, refreshToken.Status);
        Assert.Equal(Now, refreshToken.RevokedAt);

        var domainEvent = Assert.Single(refreshToken.DomainEvents, e => e.GetType() == expectedDomainEventType);
        Assert.NotNull(domainEvent);
    }

    private static void AssertTokenMarkedAsRevoked(RefreshToken refreshToken)
    {
        Assert.Equal(RefreshTokenStatus.Revoked, refreshToken.Status);
        Assert.Equal(Now, refreshToken.RevokedAt);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenDoesNotExist_ShouldReturnInvalidError()
    {
        // Arrange
        SetupGetRefreshToken(refreshToken: null);

        // Act
        var result = await _handler.Handle(Command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.RefreshToken.Invalid, left));

        await _refreshTokenRepository.Received(1)
            .GetByTokenHashAsync(HashedRefreshToken, Arg.Any<CancellationToken>());

        AssertTransactionFlow(_unitOfWork, _transaction, shouldCommit: false);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenStatusIsNotActive_ShouldReturnRevokedError()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        refreshToken.MarkAsRevokedDueToExpiration(Now);

        // Create additional active tokens that should be marked as compromised
        var activeToken1 = RefreshTokenTestData.CreateRefreshToken(userId: refreshToken.UserId);
        var activeToken2 = RefreshTokenTestData.CreateRefreshToken(userId: refreshToken.UserId);
        IReadOnlyList<RefreshToken> activeTokensInChain = [activeToken1, activeToken2];

        SetupGetRefreshToken(refreshToken);

        _refreshTokenRepository
            .GetNonExpiredActiveTokensByUserIdAsync(
                refreshToken.UserId,
                Now,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(activeTokensInChain));

        // Act
        var result = await _handler.Handle(Command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.RefreshToken.Revoked, left));

        await _refreshTokenRepository.Received(1)
            .GetByTokenHashAsync(refreshToken.TokenHash, Arg.Any<CancellationToken>());

        await _refreshTokenRepository.Received(1)
            .GetNonExpiredActiveTokensByUserIdAsync(
                refreshToken.UserId,
                Now,
                Arg.Any<CancellationToken>());

        AssertTransactionFlow(_unitOfWork, _transaction, shouldCommit: true);

        // Assert the reused token is marked as compromised with chain event
        Assert.Equal(RefreshTokenStatus.Compromised, refreshToken.Status);
        Assert.Equal(Now, refreshToken.RevokedAt);
        Assert.Single(refreshToken.DomainEvents.OfType<RefreshTokenReuseDetectedDomainEvent>());
        Assert.Single(refreshToken.DomainEvents.OfType<RefreshTokenChainCompromisedDomainEvent>());

        // Assert all tokens in the chain are also marked as compromised (without chain event)
        Assert.All(activeTokensInChain, static token =>
        {
            Assert.Equal(RefreshTokenStatus.Compromised, token.Status);
            Assert.Equal(Now, token.RevokedAt);
            Assert.Single(token.DomainEvents.OfType<RefreshTokenReuseDetectedDomainEvent>());
            Assert.Empty(token.DomainEvents.OfType<RefreshTokenChainCompromisedDomainEvent>());
        });
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenIsExpiredOnUsage_ShouldReturnExpiredError()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken(
            expiresOnUtc: Now.AddMinutes(-100) // Already expired
        );
        Assert.True(refreshToken.IsExpired(Now));

        SetupGetRefreshToken(refreshToken);

        // Act
        var result = await _handler.Handle(Command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.RefreshToken.Expired, left));

        await _refreshTokenRepository.Received(1)
            .GetByTokenHashAsync(refreshToken.TokenHash, Arg.Any<CancellationToken>());

        AssertTransactionFlow(_unitOfWork, _transaction, shouldCommit: true);

        // Assert the token is marked as revoked
        AssertTokenMarkedAsRevoked(refreshToken);
        Assert.Single(refreshToken.DomainEvents.OfType<RefreshTokenExpiredUsageDomainEvent>());
    }

    [Fact]
    public async Task Handle_WhenDeviceIdMismatch_ShouldReturnInvalidDeviceError()
    {
        // Arrange
        var differentDeviceId = Guid.NewGuid();
        var refreshToken = RefreshTokenTestData.CreateRefreshToken(
            deviceId: differentDeviceId, // Token was issued to a different device
            expiresOnUtc: Now.AddMinutes(100) // Ensure token is not expired
        );
        Assert.False(refreshToken.IsExpired(Now));

        SetupGetRefreshToken(refreshToken);

        // Act
        var result = await _handler.Handle(Command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.RefreshToken.InvalidDevice, left));

        await _refreshTokenRepository.Received(1)
            .GetByTokenHashAsync(refreshToken.TokenHash, Arg.Any<CancellationToken>());

        AssertTransactionFlow(_unitOfWork, _transaction, shouldCommit: true);

        // Assert the token is marked as compromised
        AssertTokenMarkedAsCompromised(refreshToken, typeof(RefreshTokenDeviceMismatchDetectedDomainEvent));
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        const string newAccessToken = "new-access-token";
        const string newRawRefreshToken = "new-raw-refresh-token";
        const string newRefreshTokenHash = "new-refresh-token-hash";

        var refreshTokenExpiration = TimeSpan.FromDays(7);
        var refreshToken = RefreshTokenTestData.CreateRefreshToken(
            deviceId: DeviceId,
            expiresOnUtc: Now.AddMinutes(100).ToUniversalTime() // Ensure token is not expired
        );
        Assert.False(refreshToken.IsExpired(Now));

        SetupGetRefreshToken(refreshToken);

        _jwtProvider.Create(refreshToken.User)
            .Returns(newAccessToken);

        _refreshTokenGenerator.GenerateRefreshToken()
            .Returns(new RefreshTokenResult(
                RawToken: newRawRefreshToken,
                TokenHash: newRefreshTokenHash));

        _refreshTokenGenerator.RefreshTokenExpiration
            .Returns(refreshTokenExpiration);

        // Act
        var result = await _handler.Handle(Command, CancellationToken.None);

        // Assert
        result.ShouldBeRight(right =>
        {
            Assert.Equal(newAccessToken, right.AccessToken);
            Assert.Equal(newRawRefreshToken, right.RefreshToken);
        });

        await _refreshTokenRepository.Received(1)
            .GetByTokenHashAsync(refreshToken.TokenHash, Arg.Any<CancellationToken>());

        _jwtProvider.Received(1)
            .Create(refreshToken.User);

        _refreshTokenGenerator.Received(1)
            .GenerateRefreshToken();

        _refreshTokenRepository.Received(1)
            .Insert(
                Arg.Is<RefreshToken>(rt =>
                    rt.TokenHash == newRefreshTokenHash &&
                    rt.ExpiresOnUtc == Now.Add(refreshTokenExpiration) &&
                    rt.UserId == refreshToken.UserId &&
                    rt.DeviceId == DeviceId &&
                    rt.Status == RefreshTokenStatus.Active));

        AssertTransactionFlow(_unitOfWork, _transaction, shouldCommit: true);

        // Assert the old token is marked revoked and a new token is created
        AssertTokenMarkedAsRevoked(refreshToken);
    }
}