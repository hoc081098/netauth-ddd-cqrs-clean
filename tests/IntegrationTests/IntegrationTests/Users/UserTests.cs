using LanguageExt.UnitTesting;
using Microsoft.EntityFrameworkCore;
using NetAuth.Application.Users.Register;
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
}