using LanguageExt.UnitTesting;
using NetAuth.Application.Abstractions.Cryptography;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Users.Register;
using NetAuth.Domain.Users;
using NetAuth.UnitTests.Domain.Users;
using NSubstitute;

namespace NetAuth.UnitTests.Application.Users.Register;

public class RegisterCommandHandlerTests
{
    private readonly RegisterCommandHandler _handler;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandlerTests()
    {
        // Set up
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _handler = new RegisterCommandHandler(
            _userRepository,
            _unitOfWork,
            _passwordHasher
        );
    }

    // Implement IDisposable if needed
    // public void Dispose()
    // {
    //     // Tear down
    // }

    [Fact]
    public async Task Handle_WithInvalidFormatEmail_ShouldReturnDomainError()
    {
        // Arrange
        var command = new RegisterCommand(
            Username: UserTestData.ValidUsername.Value,
            Email: "invalid-email",
            Password: UserTestData.PlainPassword);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Email.InvalidFormat, left));
        await _userRepository.DidNotReceive()
            .IsEmailUniqueAsync(email: Arg.Any<Email>(),
                cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyUsername_ShouldReturnDomainError()
    {
        // Arrange
        var command = new RegisterCommand(
            Username: "",
            Email: UserTestData.ValidEmail.Value,
            Password: UserTestData.PlainPassword);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Username.NullOrEmpty, left));
        await _userRepository.DidNotReceive()
            .IsEmailUniqueAsync(email: Arg.Any<Email>(),
                cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyPassword_ShouldReturnDomainError()
    {
        // Arrange
        var command = new RegisterCommand(
            Username: UserTestData.ValidUsername.Value,
            Email: UserTestData.ValidEmail.Value,
            Password: "");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.Password.NullOrEmpty, left));
        await _userRepository
            .DidNotReceive()
            .IsEmailUniqueAsync(email: Arg.Any<Email>(),
                cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ShouldReturnDomainError()
    {
        // Arrange
        var command = new RegisterCommand(
            Username: UserTestData.ValidUsername.Value,
            Email: UserTestData.ValidEmail.Value,
            Password: UserTestData.PlainPassword);

        _userRepository
            .IsEmailUniqueAsync(
                email: Arg.Any<Email>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.User.DuplicateEmail, left));
        await _userRepository
            .Received(1)
            .IsEmailUniqueAsync(email: Arg.Any<Email>(),
                cancellationToken: Arg.Any<CancellationToken>());
        _userRepository
            .DidNotReceive()
            .Insert(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateUserAndSave()
    {
        // Arrange
        var command = new RegisterCommand(
            Username: UserTestData.ValidUsername.Value,
            Email: UserTestData.ValidEmail.Value,
            Password: UserTestData.PlainPassword);

        _passwordHasher.HashPassword(Arg.Any<Password>())
            .Returns("hashed-password");

        _userRepository
            .IsEmailUniqueAsync(
                email: Arg.Any<Email>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(true);

        _unitOfWork.SaveChangesAsync()
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeRight(right => Assert.NotEqual(Guid.Empty, right.UserId));

        await _userRepository.Received(1)
            .IsEmailUniqueAsync(
                email: Arg.Any<Email>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _passwordHasher.Received(1)
            .HashPassword(Arg.Any<Password>());

        _userRepository.Received(1)
            .Insert(Arg.Is<User>(u =>
                u.Email == UserTestData.ValidEmail &&
                u.Username == UserTestData.ValidUsername));

        await _unitOfWork.Received(1)
            .SaveChangesAsync();
    }
}