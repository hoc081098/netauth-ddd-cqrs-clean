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

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(rt => rt.ExpiresOnUtc)
            .IsRequired();

        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.Property(rt => rt.CreatedOnUtc).IsRequired();

        builder.Property(rt => rt.ModifiedOnUtc);

        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("ux_refresh_token_token");

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("ix_refresh_token_user_id");

        builder.HasOne<User>(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId);
    }
}