using LanguageExt.UnitTesting;
using NetAuth.Application.Users.GetRoles;
using NetAuth.Domain.Users;
using NSubstitute;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace NetAuth.UnitTests.Application.Users.GetRoles;

public class GetRolesQueryHandlerTests
{
    // Subject under test (SUT)
    private readonly GetRolesQueryHandler _handler;

    // Dependencies (mocks)
    private readonly IRoleRepository _roleRepository;

    // Query
    private static readonly GetRolesQuery Query = new();

    public GetRolesQueryHandlerTests()
    {
        // Initialize mocks
        _roleRepository = Substitute.For<IRoleRepository>();

        // Initialize the handler with mocked dependencies
        _handler = new GetRolesQueryHandler(_roleRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllRoles()
    {
        // Arrange
        IReadOnlyList<Role> roles =
        [
            Role.Administrator,
            Role.Member
        ];

        _roleRepository.GetAllRolesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(roles));

        // Act
        var result = await _handler.Handle(Query, CancellationToken.None);

        // Assert
        result.ShouldBeRight(right =>
        {
            Assert.Equal(2, right.Roles.Count);
            Assert.Contains(right.Roles, r => r.Id == Role.Administrator.Id);
            Assert.Contains(right.Roles, r => r.Id == Role.Member.Id);
        });
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenRepositoryFails()
    {
        // Arrange
        _roleRepository.GetAllRolesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<Role>>(new Exception("Database error")));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(Query, CancellationToken.None));
    }
}