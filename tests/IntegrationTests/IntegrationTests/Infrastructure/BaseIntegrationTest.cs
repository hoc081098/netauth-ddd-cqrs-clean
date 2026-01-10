using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NetAuth.Infrastructure;

namespace NetAuth.IntegrationTests.Infrastructure;

[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed")]
[SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly")]
[SuppressMessage("Design", "CA1063:Implement IDisposable Correctly")]
#pragma warning disable CA1852
public class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IDisposable
#pragma warning restore CA1852
{
    /// Resolve any scoped services.
    private readonly IServiceScope _scope;

    /// To Send commands and queries to run the integration tests
    private readonly ISender _sender;

    /// Used to write any assertions
    private readonly AppDbContext _dbContext;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory webAppFactory)
    {
        Console.WriteLine($"BaseIntegrationTest@{GetHashCode()}: init");

        _scope = webAppFactory.Services.CreateScope();

        _sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    protected internal ISender Sender => _sender;
    protected internal AppDbContext DbContext => _dbContext;

    public void Dispose()
    {
        _scope.Dispose();
        Console.WriteLine($"BaseIntegrationTest@{GetHashCode()}: disposed");
        GC.SuppressFinalize(this);
    }
}