using LanguageExt.UnitTesting;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Users.Login;
using NetAuth.Domain.Users;
using NetAuth.UnitTests.Application.Abstractions.Common;
using NetAuth.UnitTests.Domain.Users;
using NSubstitute;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace NetAuth.UnitTests.Application.Users.Login;

public class LoginCommandHandlerTests
{
    // Fixed point in time for consistent test results
    private static readonly DateTimeOffset UtcNow =
        new(2025, 01, 01, 0, 0, 0, TimeSpan.Zero);

    // Subject under test (SUT)
    private readonly LoginCommandHandler _handler;

    // Dependencies
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHashChecker _passwordHashChecker;
    private readonly IJwtProvider _jwtProvider;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    // Test data
    private static readonly Email Email = UserTestData.ValidEmail;

    private static readonly LoginCommand Command = new(
        Email: Email.Value,
        Password: UserTestData.PlainPassword,
        DeviceId: Guid.NewGuid());

    public LoginCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _passwordHashChecker = Substitute.For<IPasswordHashChecker>();
        _jwtProvider = Substitute.For<IJwtProvider>();
        _refreshTokenGenerator = Substitute.For<IRefreshTokenGenerator>();
        _clock = FixedClock.CreateWithUtcNow(UtcNow);
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new LoginCommandHandler(
            _userRepository,
            _refreshTokenRepository,
            _passwordHashChecker,
            _jwtProvider,
            _refreshTokenGenerator,
            _clock,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithInvalidFormatEmail_ShouldReturnDomainError()
    {
        // Arrange
        var command = Command with { Email = "invalid-email" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Email.InvalidFormat, left));

        await _userRepository
            .DidNotReceiveWithAnyArgs()
            .GetByEmailAsync(null!, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldReturnInvalidCredentialsError()
    {
        // Arrange
        _userRepository.GetByEmailAsync(
                email: Arg.Any<Email>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(null));

        // Act
        var result = await _handler.Handle(Command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.User.InvalidCredentials, left));

        await _userRepository.Received(1)
            .GetByEmailAsync(
                email: Arg.Is<Email>(email => email == Email),
                cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPasswordIsIncorrect_ShouldReturnInvalidCredentialsError()
    {
        // Arrange
        var user = User.Create(
            email: Email,
            username: UserTestData.ValidUsername,
            passwordHash: UserTestData.PlainPassword // Use plain password as hash for testing
        );

        _userRepository.GetByEmailAsync(
                email: Arg.Any<Email>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));

        _passwordHashChecker.IsMatch(passwordHash: Arg.Any<string>(), providedPassword: Command.Password)
            .Returns(false);

        // Act
        var result = await _handler.Handle(Command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.User.InvalidCredentials, left));

        await _userRepository.Received(1)
            .GetByEmailAsync(
                email: Arg.Is<Email>(e => e == Email),
                cancellationToken: Arg.Any<CancellationToken>());

        _passwordHashChecker.Received(1)
            .IsMatch(Arg.Any<string>(), Command.Password);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnTokensAndPersistRefreshToken()
    {
        // Arrange
        var user = User.Create(
            email: Email,
            username: UserTestData.ValidUsername,
            passwordHash: UserTestData.PlainPassword // Use plain password as hash for testing
        );

        const string expectedAccessToken = "access-token";
        const string expectedRawRefreshToken = "raw";

        _userRepository.GetByEmailAsync(
                email: Arg.Any<Email>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));

        _passwordHashChecker.IsMatch(passwordHash: Arg.Any<string>(), providedPassword: Command.Password)
            .Returns(true);

        _jwtProvider.Create(user)
            .Returns(expectedAccessToken);

        _refreshTokenGenerator.GenerateRefreshToken()
            .Returns(new RefreshTokenResult(RawToken: expectedRawRefreshToken, TokenHash: "hash"));

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(Command, CancellationToken.None);

        // Assert
        result.ShouldBeRight(right =>
        {
            Assert.Equal(expectedAccessToken, right.AccessToken);
            Assert.Equal(expectedRawRefreshToken, right.RefreshToken);
        });

        await _userRepository.Received(1)
            .GetByEmailAsync(
                email: Arg.Is<Email>(e => e == Email),
                cancellationToken: Arg.Any<CancellationToken>());

        _passwordHashChecker.Received(1)
            .IsMatch(Arg.Any<string>(), Command.Password);

        _jwtProvider.Received(1)
            .Create(user);

        _refreshTokenGenerator.Received(1)
            .GenerateRefreshToken();

        _refreshTokenRepository.Received(1)
            .Insert(Arg.Is<RefreshToken>(rt => rt.UserId == user.Id));

        await _unitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}