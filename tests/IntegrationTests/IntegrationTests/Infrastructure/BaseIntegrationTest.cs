using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NetAuth.Infrastructure;

namespace NetAuth.IntegrationTests.Infrastructure;

[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed")]
[SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly")]
#pragma warning disable CA1852
internal class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IDisposable
#pragma warning restore CA1852
{
    private readonly IServiceScope _scope;

    protected readonly ISender Sender;
    protected readonly AppDbContext DbContext;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory webAppFactory)
    {
        Console.WriteLine($"BaseIntegrationTest@{GetHashCode()}: init");

        _scope = webAppFactory.Services.CreateScope();

        Sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        DbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        Console.WriteLine($"BaseIntegrationTest@{GetHashCode()}: disposed");
    }
}