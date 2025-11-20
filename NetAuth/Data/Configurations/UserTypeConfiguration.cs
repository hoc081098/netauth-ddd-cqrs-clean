using System.Globalization;
using EFCore.NamingConventions.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetAuth.Domain.Users;

namespace NetAuth.Data.Configurations;

public class UserTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        var snakeCaseNameRewriter = new SnakeCaseNameRewriter(CultureInfo.InvariantCulture);
        
        builder.OwnsOne(user => user.Email, emailBuilder =>
        {
            emailBuilder.Property(email => email.Value)
                .HasColumnName(snakeCaseNameRewriter.RewriteName(nameof(User.Email)))
                .IsRequired()
                .HasMaxLength(Email.MaxLength);
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

        // Query filter to exclude soft-deleted users for any queries targeting the User entity
        builder.HasQueryFilter(user => !user.IsDeleted);
    }
}