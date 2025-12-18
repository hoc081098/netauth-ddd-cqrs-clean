using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Domain.Core.Abstractions;

namespace NetAuth.Infrastructure.Interceptors;

internal sealed class AuditableEntityInterceptor(IClock clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditableEntities(DbContext? eventDataContext)
    {
        if (eventDataContext is null) return;

        var utcNow = clock.UtcNow;
        foreach (var entityEntry in eventDataContext.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entityEntry.State is EntityState.Added)
            {
                entityEntry.Property(p => p.CreatedOnUtc).CurrentValue = utcNow;
            }

            if (entityEntry.State is EntityState.Added or EntityState.Modified ||
                entityEntry.HasChangedOwnedEntities())
            {
                entityEntry.Property(p => p.ModifiedOnUtc).CurrentValue = utcNow;
            }
        }
    }
}

internal static class EntryExtensions
{
    internal static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(r =>
        {
            var targetEntry = r.TargetEntry;
            return targetEntry is not null &&
                   targetEntry.Metadata.IsOwned() &&
                   (targetEntry.State is EntityState.Added or EntityState.Modified);
        });
}