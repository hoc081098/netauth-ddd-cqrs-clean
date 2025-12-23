using LanguageExt.UnitTesting;
using NetAuth.Application.Abstractions.Authentication;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Users.Login;
using NetAuth.Domain.Users;
using NetAuth.UnitTests.Application.Abstractions.Common;
using NetAuth.UnitTests.Domain.Users;
using NSubstitute;

namespace NetAuth.UnitTests.Application.Users.Login;

public class LoginCommandHandlerTests
{
    private readonly LoginCommandHandler _handler;

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHashChecker _passwordHashChecker;
    private readonly IJwtProvider _jwtProvider;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly DateTimeOffset Now = DateTimeOffset.Now;

    public LoginCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _passwordHashChecker = Substitute.For<IPasswordHashChecker>();
        _jwtProvider = Substitute.For<IJwtProvider>();
        _refreshTokenGenerator = Substitute.For<IRefreshTokenGenerator>();
        _clock = FixedClock.Create(Now);
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
        var command = new LoginCommand(
            Email: "invalid-email",
            Password: UserTestData.PlainPassword,
            DeviceId: "device-123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Email.InvalidFormat, left));
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldReturnInvalidCredentialsError()
    {
        // Arrange
        var validEmail = UserTestData.ValidEmail;
        var command = new LoginCommand(
            Email: validEmail.Value,
            Password: UserTestData.PlainPassword,
            DeviceId: "device-123");

        _userRepository.GetByEmailAsync(
                email: Arg.Any<Email>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.User.InvalidCredentials, left));

        await _userRepository.Received(1)
            .GetByEmailAsync(
                email: Arg.Is<Email>(email => email == validEmail),
                cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPasswordIsIncorrect_ShouldReturnInvalidCredentialsError()
    {
        // Arrange
        var email = UserTestData.ValidEmail;

        var user = User.Create(
            email: email,
            username: UserTestData.ValidUsername,
            passwordHash: UserTestData.PlainPassword // Use plain password as hash for testing
        );

        var command = new LoginCommand(
            Email: email.Value,
            Password: UserTestData.PlainPassword,
            DeviceId: "device-123");

        _userRepository.GetByEmailAsync(
                email: Arg.Any<Email>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));

        _passwordHashChecker.IsMatch(passwordHash: Arg.Any<string>(), providedPassword: command.Password)
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.User.InvalidCredentials, left));

        await _userRepository.Received(1)
            .GetByEmailAsync(
                email: Arg.Is<Email>(e => e == email),
                cancellationToken: Arg.Any<CancellationToken>());

        _passwordHashChecker.Received(1)
            .IsMatch(Arg.Any<string>(), command.Password);
    }
}