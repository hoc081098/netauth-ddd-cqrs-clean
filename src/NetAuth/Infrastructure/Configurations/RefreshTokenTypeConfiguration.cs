using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Configurations;

internal sealed class RefreshTokenTypeConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(rt => rt.ExpiresOnUtc)
            .IsRequired();

        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.Property(rt => rt.DeviceId)
            .IsRequired();

        builder.Property(rt => rt.RevokedAt);

        builder.Property(rt => rt.Status)
            .IsRequired();

        builder.Property(rt => rt.ReplacedById);

        builder.Property(rt => rt.CreatedOnUtc).IsRequired();

        builder.Property(rt => rt.ModifiedOnUtc);

        // -------------------- Indexes & Relationships --------------------

        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_refresh_token_token");

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("ix_refresh_token_user_id");

        builder.HasOne<User>(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId);

        // Query filter to exclude soft-deleted users for any queries targeting the User entity
        builder.HasQueryFilter(rt => !rt.User.IsDeleted);

        // Each refresh token can be replaced by another refresh token (one-to-one relationship)
        builder.HasOne<RefreshToken>(rt => rt.ReplacedBy)
            .WithOne() // ReplacedBy points to the new token, but the new token doesn't need a back-reference to the old one
            .HasForeignKey<RefreshToken>(rt => rt.ReplacedById) // Foreign key is stored in the OLD record
            .OnDelete(DeleteBehavior.SetNull); // Critical: prevents cascading deletes
    }
}