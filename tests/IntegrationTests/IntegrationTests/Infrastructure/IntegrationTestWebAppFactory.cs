using System.Diagnostics.CodeAnalysis;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetAuth.Infrastructure;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace NetAuth.IntegrationTests.Infrastructure;

// References: https://www.milanjovanovic.tech/blog/testcontainers-integration-testing-using-docker-in-dotnet
// ASP.NET Core provides an in-memory test server that we can use to spin up an application instance for running tests.
// The Microsoft.AspNetCore.Mvc.Testing package provides the WebApplicationFactory class
// that we will use as the base for our implementation.

// The custom IntegrationTestWebAppFactory will do a few things:
// 
// - Create and configure Container instances.
// - Call ConfigureTestServices to set up EF Core with the container database
// - Start and stop the container instance with IAsyncLifetime
[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed")]
public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-alpine")
        .WithName("NetAuth.Database")
        .WithDatabase("NetAuth")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:8.4.0-alpine")
        .WithName("NetAuth.Redis")
        .Build();

    private readonly IContainer _seqContainer = new ContainerBuilder("datalust/seq:latest")
        .WithName("NetAuth.Seq")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithEnvironment("SEQ_FIRSTRUN_ADMINUSERNAME", "admin")
        .WithEnvironment("SEQ_FIRSTRUN_ADMINPASSWORD", "123456")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // ConfigureTestServices came from Microsoft.AspNetCore.TestHost
        builder.ConfigureTestServices(services =>
        {
            // Wiring up any dependencies on the database container
            services.RemoveAll<NpgsqlDataSource>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            var dbConnectionString = _dbContainer.GetConnectionString();
            services.AddSingleton(_ => new NpgsqlDataSourceBuilder(dbConnectionString).Build());
            services.AddDbContext<AppDbContext>((serviceProvider, optionsBuilder) =>
                optionsBuilder
                    .UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>())
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>()));

            services.Configure<RedisCacheOptions>(redisCacheOptions =>
                redisCacheOptions.Configuration = _redisContainer.GetConnectionString());
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();
        await _seqContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _redisContainer.StopAsync();
        await _seqContainer.StopAsync();
        await base.DisposeAsync();
    }
}