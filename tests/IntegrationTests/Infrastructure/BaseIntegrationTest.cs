using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NetAuth.Infrastructure;
using Xunit.Abstractions;

namespace NetAuth.IntegrationTests.Infrastructure;

[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed")]
[SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly")]
[SuppressMessage("Design", "CA1063:Implement IDisposable Correctly")]
#pragma warning disable CA1852
public class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IDisposable
#pragma warning restore CA1852
{
    private readonly ITestOutputHelper _testOutputHelper;

    /// Resolve any scoped services.
    private readonly IServiceScope _scope;

    protected BaseIntegrationTest(
        IntegrationTestWebAppFactory webAppFactory,
        ITestOutputHelper testOutputHelper
    )
    {
        _testOutputHelper = testOutputHelper;
        testOutputHelper.WriteLine(
            $"BaseIntegrationTest@{RuntimeHelpers.GetHashCode(this)}: init");

        // Since webAppFactory.Services is shared across tests,
        // so we need to create a scope for each test instance.
        _scope = webAppFactory.Services.CreateScope();

        Sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        DbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        TestUserContext = _scope.ServiceProvider.GetRequiredService<TestUserContext>();
    }

    /// To Send commands and queries to run the integration tests
    protected ISender Sender { get; }

    /// Used to write any assertions
    protected AppDbContext DbContext { get; }

    /// Test user context for simulating authenticated users
    protected TestUserContext TestUserContext { get; }

    public void Dispose()
    {
        _scope.Dispose();
        _testOutputHelper.WriteLine($"BaseIntegrationTest@{RuntimeHelpers.GetHashCode(this)}: disposed");
        GC.SuppressFinalize(this);
    }
}