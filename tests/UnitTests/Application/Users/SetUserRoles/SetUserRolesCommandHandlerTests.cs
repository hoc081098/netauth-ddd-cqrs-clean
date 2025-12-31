using LanguageExt.UnitTesting;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Application.Users.SetUserRoles;
using NetAuth.Domain.Users;
using NetAuth.UnitTests.Domain.Users;
using NSubstitute;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace NetAuth.UnitTests.Application.Users.SetUserRoles;

public class SetUserRolesCommandHandlerTests
{
    // Subject under test (SUT)
    private readonly SetUserRolesCommandHandler _handler;

    // Dependencies
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Test data

    public SetUserRolesCommandHandlerTests()
    {
        _roleRepository = Substitute.For<IRoleRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new SetUserRolesCommandHandler(
            _roleRepository,
            _userRepository,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldReturnUserNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new SetUserRolesCommand(
            userId,
            [1, 2],
            RoleChangeActor.System);

        _userRepository.GetByIdAsyncWithRoles(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.User.NotFound, left));

        await _userRepository.Received(1)
            .GetByIdAsyncWithRoles(userId, Arg.Any<CancellationToken>());

        await _unitOfWork.DidNotReceiveWithAnyArgs()
            .SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WhenSomeRolesDoNotExist_ShouldReturnOneOrMoreRolesNotFoundError()
    {
        // Arrange
        IReadOnlySet<RoleId> roleIds = new HashSet<RoleId> { RoleId.AdministratorId, RoleId.MemberId };
        var user = User.Create(
            UserTestData.ValidEmail,
            UserTestData.ValidUsername,
            UserTestData.PlainPassword // Use plain password as hash for testing
        );
        var userId = user.Id;
        var command = new SetUserRolesCommand(
            userId,
            roleIds.Select(r => r.Value).ToArray(),
            RoleChangeActor.System);

        _userRepository.GetByIdAsyncWithRoles(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));

        _roleRepository.GetRolesByIdsAsync(Arg.Any<IReadOnlySet<RoleId>>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<IReadOnlyList<Role>>([
                    Role.Administrator // Only one role returned instead of two
                ])
            );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left => Assert.Equal(UsersDomainErrors.User.OneOrMoreRolesNotFound, left));

        await _userRepository.Received(1)
            .GetByIdAsyncWithRoles(userId, Arg.Any<CancellationToken>());

        await _roleRepository.Received(1)
            .GetRolesByIdsAsync(
                Arg.Is<IReadOnlySet<RoleId>>(actualRoleIds => actualRoleIds.SetEquals(roleIds)),
                Arg.Any<CancellationToken>());

        await _unitOfWork.DidNotReceiveWithAnyArgs()
            .SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WhenAllInputsAreValidAndSetRolesSucceeds_ShouldSetRolesAndSaveChanges()
    {
        // Arrange
        IReadOnlySet<RoleId> newRoleIds = new HashSet<RoleId> { RoleId.AdministratorId, RoleId.MemberId };
        var user = User.Create(
            UserTestData.ValidEmail,
            UserTestData.ValidUsername,
            UserTestData.PlainPassword // Use plain password as hash for testing
        );
        var userId = user.Id;
        var command = new SetUserRolesCommand(
            userId,
            newRoleIds.Select(r => r.Value).ToArray(),
            RoleChangeActor.System);

        _userRepository.GetByIdAsyncWithRoles(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));

        _roleRepository.GetRolesByIdsAsync(Arg.Any<IReadOnlySet<RoleId>>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<IReadOnlyList<Role>>([
                    Role.Administrator,
                    Role.Member
                ])
            );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeRight();

        Assert.True(
            user.Roles.Select(r => r.Id)
                .ToHashSet()
                .SetEquals(newRoleIds));

        await _userRepository.Received(1)
            .GetByIdAsyncWithRoles(userId, Arg.Any<CancellationToken>());

        await _roleRepository.Received(1)
            .GetRolesByIdsAsync(
                Arg.Is<IReadOnlySet<RoleId>>(actualRoleIds => actualRoleIds.SetEquals(newRoleIds)),
                Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAllowedToGrantAdminRole_ShouldReturnDomainError()
    {
        // Arrange
        IReadOnlySet<RoleId> newRoleIds = new HashSet<RoleId> { RoleId.AdministratorId };
        var user = User.Create(
            UserTestData.ValidEmail,
            UserTestData.ValidUsername,
            UserTestData.PlainPassword // Use plain password as hash for testing
        );
        Assert.Single(user.Roles, r => r.Id == RoleId.MemberId);

        var userId = user.Id;
        var command = new SetUserRolesCommand(
            userId,
            newRoleIds.Select(r => r.Value).ToArray(),
            RoleChangeActor.User); // User actor trying to assign admin role

        _userRepository.GetByIdAsyncWithRoles(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));

        _roleRepository.GetRolesByIdsAsync(Arg.Any<IReadOnlySet<RoleId>>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<IReadOnlyList<Role>>([
                    Role.Administrator
                ])
            );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeLeft(left =>
            Assert.Equal(UsersDomainErrors.User.CannotGrantAdminRole, left));

        // User roles should remain unchanged
        Assert.Single(user.Roles, r => r.Id == RoleId.MemberId);

        await _userRepository.Received(1)
            .GetByIdAsyncWithRoles(userId, Arg.Any<CancellationToken>());

        await _roleRepository.Received(1)
            .GetRolesByIdsAsync(
                Arg.Is<IReadOnlySet<RoleId>>(actualRoleIds => actualRoleIds.SetEquals(newRoleIds)),
                Arg.Any<CancellationToken>());

        await _unitOfWork.DidNotReceiveWithAnyArgs()
            .SaveChangesAsync(CancellationToken.None);
    }
}