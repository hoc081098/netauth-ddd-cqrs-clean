using NetArchTest.Rules;

namespace NetAuth.ArchitectureTests;

internal static class Namespaces
{
    internal const string Domain = "NetAuth.Domain";
    internal const string Application = "NetAuth.Application";
    internal const string Infrastructure = "NetAuth.Infrastructure";
    internal const string WebApi = "NetAuth.Web.Api";
}

public class LayerTest
{
    #region Domain Layer Tests

    [Fact]
    public void DomainLayer_ShouldNotHaveDependencyOnApplication()
    {
        var result = Types
            .InAssembly(typeof(Program).Assembly)
            .That()
            .ResideInNamespace(Namespaces.Domain)
            .Should()
            .NotHaveDependencyOn(Namespaces.Application)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void DomainLayer_ShouldNotHaveDependencyOn_InfrastructureLayer()
    {
        var result = Types
            .InAssembly(typeof(Program).Assembly)
            .That()
            .ResideInNamespace(Namespaces.Domain)
            .Should()
            .NotHaveDependencyOn(Namespaces.Infrastructure)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void DomainLayer_ShouldNotHaveDependencyOn_WebApiLayer()
    {
        var result = Types
            .InAssembly(typeof(Program).Assembly)
            .That()
            .ResideInNamespace(Namespaces.Domain)
            .Should()
            .NotHaveDependencyOn(Namespaces.WebApi)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    #endregion

    #region Application Layer Tests

    [Fact]
    public void ApplicationLayer_ShouldNotHaveDependencyOn_InfrastructureLayer()
    {
        var result = Types
            .InAssembly(typeof(Program).Assembly)
            .That()
            .ResideInNamespace(Namespaces.Application)
            .Should()
            .NotHaveDependencyOn(Namespaces.Infrastructure)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void ApplicationLayer_ShouldNotHaveDependencyOn_WebApiLayer()
    {
        var result = Types
            .InAssembly(typeof(Program).Assembly)
            .That()
            .ResideInNamespace(Namespaces.Application)
            .Should()
            .NotHaveDependencyOn(Namespaces.WebApi)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    #endregion

    [Fact]
    public void InfrastructureLayer_ShouldNotHaveDependencyOn_WebApiLayer()
    {
        var result = Types
            .InAssembly(typeof(Program).Assembly)
            .That()
            .ResideInNamespace(Namespaces.Infrastructure)
            .Should()
            .NotHaveDependencyOn(Namespaces.WebApi)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}