using System.Diagnostics.CodeAnalysis;
using LanguageExt.UnitTesting;
using NetAuth.Application.Users.GetUserRoles;
using NetAuth.Domain.Users;
using NSubstitute;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace NetAuth.UnitTests.Application.Users.GetUserRoles;

[SuppressMessage("Usage", "CA2201:Do not raise reserved exception types")]
public class GetUserRolesQueryHandlerTests
{
    // Subject under test (SUT)
    private readonly GetUserRolesQueryHandler _handler;

    // Dependencies (mocks)
    private readonly IRoleRepository _roleRepository;

    // Test data
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly GetUserRolesQuery Query = new(UserId);

    public GetUserRolesQueryHandlerTests()
    {
        // Initialize mocks
        _roleRepository = Substitute.For<IRoleRepository>();

        // Initialize the handler with mocked dependencies
        _handler = new GetUserRolesQueryHandler(_roleRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnUserRoles()
    {
        // Arrange
        IReadOnlyList<Role> roles =
        [
            Role.Administrator,
            Role.Member
        ];

        _roleRepository.GetRolesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
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

        await _roleRepository.Received(1)
            .GetRolesByUserIdAsync(UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenUserHasNoRoles()
    {
        // Arrange
        IReadOnlyList<Role> roles = [];

        _roleRepository.GetRolesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(roles));

        // Act
        var result = await _handler.Handle(Query, CancellationToken.None);

        // Assert
        result.ShouldBeRight(right => Assert.Empty(right.Roles));

        await _roleRepository.Received(1)
            .GetRolesByUserIdAsync(UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenRepositoryFails()
    {
        // Arrange
        _roleRepository.GetRolesByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<Role>>(new Exception("Database error")));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(Query, CancellationToken.None));

        await _roleRepository.Received(1)
            .GetRolesByUserIdAsync(UserId, Arg.Any<CancellationToken>());
    }
}