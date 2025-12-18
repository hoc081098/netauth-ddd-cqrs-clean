using System.Globalization;
using EFCore.NamingConventions.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetAuth.Domain.Users;
using NetAuth.Infrastructure.Models;

namespace NetAuth.Infrastructure.Configurations;

public class UserTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        var snakeCaseNameRewriter = new SnakeCaseNameRewriter(CultureInfo.InvariantCulture);

        builder.HasKey(user => user.Id);

        builder.OwnsOne(user => user.Email, emailBuilder =>
        {
            emailBuilder.Property(email => email.Value)
                .HasColumnName(snakeCaseNameRewriter.RewriteName(nameof(User.Email)))
                .IsRequired()
                .HasMaxLength(Email.MaxLength);

            emailBuilder.HasIndex(e => e.Value)
                .IsUnique()
                .HasDatabaseName("ux_user_email");
        });

        builder.OwnsOne(user => user.Username, usernameBuilder =>
        {
            usernameBuilder.Property(username => username.Value)
                .HasColumnName(snakeCaseNameRewriter.RewriteName(nameof(User.Username)))
                .IsRequired()
                .HasMaxLength(Username.MaxLength);
        });

        builder.Property<string>("_passwordHash")
            .HasField("_passwordHash")
            .HasColumnName("password_hash")
            .IsRequired();

        builder.Property(user => user.CreatedOnUtc).IsRequired();

        builder.Property(user => user.ModifiedOnUtc);

        builder.Property(user => user.DeletedOnUtc);

        builder.Property(user => user.IsDeleted).HasDefaultValue(false);

        // -------------------- Indexes & Relationships --------------------

        // Query filter to exclude soft-deleted users for any queries targeting the User entity
        builder.HasQueryFilter(user => !user.IsDeleted);

        builder.HasMany(user => user.Roles)
            .WithMany()
            .UsingEntity<RoleUser>(
                configureRight: roleUserBuilder =>
                    roleUserBuilder.HasOne<Role>(roleUser => roleUser.Role)
                        .WithMany()
                        .HasForeignKey(roleUser => roleUser.RoleId),
                configureLeft: roleUserBuilder =>
                    roleUserBuilder.HasOne<User>()
                        .WithMany()
                        .HasForeignKey(roleUser => roleUser.UserId),
                configureJoinEntityType: roleUserBuilder =>
                {
                    roleUserBuilder.Property(roleUser => roleUser.UserId);

                    roleUserBuilder.Property(roleUser => roleUser.RoleId)
                        .HasConversion(
                            id => id.Value,
                            value => new RoleId(value)
                        );
                }
            );

        // Configure the navigation property to use field access
        builder.Navigation(u => u.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}