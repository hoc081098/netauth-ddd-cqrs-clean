using LanguageExt.UnitTesting;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Users.LoginWithRefreshToken;
using NetAuth.Domain.Users;
using NetAuth.UnitTests.Application.Abstractions.Common;
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

    public LoginWithRefreshTokenCommandHandlerTests()
    {
        // Set up
        _refreshTokenGenerator = Substitute.For<IRefreshTokenGenerator>();
        _jwtProvider = Substitute.For<IJwtProvider>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _clock = FixedClock.Create(Now);
        _unitOfWork = Substitute.For<IUnitOfWork>();

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
        ;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.RefreshToken.Invalid, left));
    }
}