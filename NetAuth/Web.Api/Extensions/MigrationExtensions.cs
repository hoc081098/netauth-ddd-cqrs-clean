using Microsoft.EntityFrameworkCore;
using NetAuth.Infrastructure;

namespace NetAuth.Web.Api.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        appDbContext.Database.Migrate();
    }
}