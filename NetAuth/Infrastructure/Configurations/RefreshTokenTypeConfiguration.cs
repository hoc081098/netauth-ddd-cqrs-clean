using System.Globalization;
using EFCore.NamingConventions.Internal;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Configurations;

internal sealed class RefreshTokenTypeConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        var snakeCaseNameRewriter = new SnakeCaseNameRewriter(CultureInfo.InvariantCulture);
        builder.ToTable(snakeCaseNameRewriter.RewriteName(nameof(RefreshToken).Pluralize()));

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(rt => rt.ExpiresOnUtc)
            .IsRequired();

        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.Property(rt => rt.DeviceId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(rt => rt.RevokedAt);

        builder.Property(rt => rt.Status)
            .IsRequired();

        builder.Property(rt => rt.ReplacedById);

        builder.Property(rt => rt.CreatedOnUtc).IsRequired();

        builder.Property(rt => rt.ModifiedOnUtc);

        // -- Indexes & Relationships

        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_refresh_token_token");

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("ix_refresh_token_user_id");

        builder.HasOne<User>(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId);

        // Each refresh token can be replaced by another refresh token (one-to-one relationship)
        builder.HasOne<RefreshToken>(rt => rt.ReplacedBy)
            .WithOne()
            .HasForeignKey<RefreshToken>(rt => rt.ReplacedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}