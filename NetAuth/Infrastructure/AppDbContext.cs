using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NetAuth.Application.Abstractions.Common;
using NetAuth.Application.Abstractions.Data;
using NetAuth.Domain.Core.Primitives;
using NetAuth.Infrastructure.Outbox;

namespace NetAuth.Infrastructure;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IClock clock
) : DbContext(options),
    IUnitOfWork
{
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

        var outboxMessages = ChangeTracker.Entries<AggregateRoot>()
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
                Type = domainEvent.GetType().Name,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                OccurredOnUtc = utcNow,
                ProcessedOnUtc = null,
                Error = null
            })
            .ToArray();
        
        Set<OutboxMessage>().AddRange(outboxMessages);
    }
}