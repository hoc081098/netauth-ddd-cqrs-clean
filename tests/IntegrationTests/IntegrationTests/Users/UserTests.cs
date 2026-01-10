using LanguageExt.UnitTesting;
using Microsoft.EntityFrameworkCore;
using NetAuth.Application.Users.Register;
using NetAuth.Domain.Users;
using NetAuth.IntegrationTests.Infrastructure;

namespace NetAuth.IntegrationTests.Users;

public class UserTests(IntegrationTestWebAppFactory webAppFactory) : BaseIntegrationTest(webAppFactory)
{
    [Fact]
    public async Task Register_ShouldCreateNewUser()
    {
        // Arrange
        var registerCommand = new RegisterCommand(
            Username: "hoc081098",
            Email: "hoc081098@gmail.com",
            Password: "123456Aa@"
        );

        // Act
        var result = await Sender.Send(registerCommand);

        // Assert
        result.ShouldBeRight();

        var created = await DbContext
            .Users
            .FirstOrDefaultAsync(u => u.Email.Value == registerCommand.Email);
        Assert.NotNull(created);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldFail()
    {
        // Arrange
        const string email = "hoc081098_01@gmail.com";

        var registerCommand1 = new RegisterCommand(
            Username: "hoc081098_01",
            Email: email,
            Password: "123456Aa@");
        var registerCommand2 = new RegisterCommand(
            Username: "another-username",
            Email: email,
            Password: "123456Aa@");

        // Act
        var result1 = await Sender.Send(registerCommand1);
        var result2 = await Sender.Send(registerCommand2);

        // Assert
        result1.ShouldBeRight();
        result2.ShouldBeLeft(left =>
            Assert.Equal(UsersDomainErrors.User.DuplicateEmail, left));

        var count = await DbContext
            .Users
            .CountAsync(u => u.Email.Value == registerCommand1.Email);
        Assert.Equal(1, count);
    }
}