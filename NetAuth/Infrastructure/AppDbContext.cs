using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Domain.Users;
using NetAuth.Infrastructure.Models;
using NetAuth.Infrastructure.Outbox;

namespace NetAuth.Infrastructure;

internal sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IClock clock
) : DbContext(options),
    IUnitOfWork
{
    // Expose DbSet<TEntity> properties for each entity so EF Core's conventions produce pluralized table names.

    internal DbSet<User> Users => Set<User>();
    internal DbSet<Role> Roles => Set<Role>();
    internal DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    internal DbSet<RoleUser> RoleUsers => Set<RoleUser>();
    internal DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    internal DbSet<Permission> Permissions => Set<Permission>();
    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Database.BeginTransactionAsync(cancellationToken);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // First process the domain events and add them to ChangeTracker as Outbox Messages,
        // then persist everything in the database in a single transaction "atomic operation" 
        AddDomainEventsAsOutboxMessages();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void AddDomainEventsAsOutboxMessages()
    {
        var utcNow = clock.UtcNow;

        var outboxMessages = ChangeTracker.Entries<IAggregateRoot>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                var events = entity.DomainEvents;
                entity.ClearDomainEvents();
                return events;
            })
            .Select(domainEvent => new OutboxMessage
            {
                Id = Guid.CreateVersion7(),
                Type = domainEvent.GetType().FullName!,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                OccurredOnUtc = utcNow,
                ProcessedOnUtc = null,
                Error = null
            })
            .ToArray();

        OutboxMessages.AddRange(outboxMessages);
    }
}