using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Domain.Core.Abstractions;

namespace NetAuth.Infrastructure.Interceptors;

internal sealed class SoftDeletableEntityInterceptor(IClock clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateSoftDeletableEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateSoftDeletableEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateSoftDeletableEntities(DbContext? eventDataContext)
    {
        if (eventDataContext is null) return;

        var utcNow = clock.UtcNow;
        foreach (var entityEntry in eventDataContext.ChangeTracker.Entries<ISoftDeletableEntity>())
        {
            // Only process entities with state Deleted
            if (entityEntry.State is not EntityState.Deleted) continue;

            entityEntry.Property(p => p.DeletedOnUtc).CurrentValue = utcNow;
            entityEntry.Property(p => p.IsDeleted).CurrentValue = true;
            entityEntry.State = EntityState.Modified; // Change state to Modified to perform soft delete

            UpdateDeletedEntityEntryReferencesToUnchanged(entityEntry);
        }
    }

    /// <summary>
    /// Updates the specified entity entry's referenced entries in the deleted state to the modified state.
    /// This method is recursive.
    /// </summary>
    /// <param name="entityEntry">The entity entry.</param>
    private static void UpdateDeletedEntityEntryReferencesToUnchanged(EntityEntry entityEntry)
    {
        if (!entityEntry.References.Any())
        {
            return;
        }

        foreach (
            var referenceEntry in entityEntry
                .References
                .Where(r => r.TargetEntry?.State is EntityState.Deleted))
        {
            var entry = referenceEntry.TargetEntry;
            if (entry is null) continue;

            entry.State = EntityState.Unchanged;
            UpdateDeletedEntityEntryReferencesToUnchanged(entry);
        }
    }
}