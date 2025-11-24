using System.Globalization;
using EFCore.NamingConventions.Internal;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetAuth.Infrastructure.Outbox;

namespace NetAuth.Infrastructure.Configurations;

internal sealed class OutboxMessageTypeConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        var snakeCaseNameRewriter = new SnakeCaseNameRewriter(CultureInfo.InvariantCulture);

        builder.ToTable(snakeCaseNameRewriter.RewriteName(nameof(OutboxMessage).Pluralize()));

        builder.HasKey(outboxMessage => outboxMessage.Id);

        builder.Property(outboxMessage => outboxMessage.Type)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(outboxMessage => outboxMessage.Content)
            .HasColumnType("jsonb")
            .IsRequired();

        builder
            .Property(outboxMessage => outboxMessage.OccurredOnUtc)
            .IsRequired();

        builder.Property(o => o.ProcessedOnUtc);

        builder.Property(o => o.Error);

        var processedOnUtcColumnName = snakeCaseNameRewriter.RewriteName(nameof(OutboxMessage.ProcessedOnUtc));

        builder
            .HasIndex(outboxMessage => new { outboxMessage.OccurredOnUtc, outboxMessage.ProcessedOnUtc })
            .HasDatabaseName("idx_outbox_messages_unprocessed")
            .IncludeProperties(outboxMessage => new { outboxMessage.Id, outboxMessage.Type, outboxMessage.Content })
            .HasFilter($"\"{processedOnUtcColumnName}\" IS NULL");
    }
}