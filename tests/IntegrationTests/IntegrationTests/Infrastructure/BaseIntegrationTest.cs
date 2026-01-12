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

    /// To Send commands and queries to run the integration tests
    private readonly ISender _sender;

    /// Used to write any assertions
    private readonly AppDbContext _dbContext;

    protected BaseIntegrationTest(
        IntegrationTestWebAppFactory webAppFactory,
        ITestOutputHelper testOutputHelper
    )
    {
        _testOutputHelper = testOutputHelper;
        testOutputHelper.WriteLine(
            $"BaseIntegrationTest@{RuntimeHelpers.GetHashCode(this)}: init");

        _scope = webAppFactory.Services.CreateScope();

        _sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    protected ISender Sender => _sender;
    protected AppDbContext DbContext => _dbContext;

    public void Dispose()
    {
        _scope.Dispose();
        _testOutputHelper.WriteLine($"BaseIntegrationTest@{RuntimeHelpers.GetHashCode(this)}: disposed");
        GC.SuppressFinalize(this);
    }
}