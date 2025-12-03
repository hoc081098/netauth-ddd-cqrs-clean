using System.Globalization;
using EFCore.NamingConventions.Internal;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Configurations;

public class PermissionTypeConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(
                id => id.Value,
                value => new PermissionId(value)
            )
            .ValueGeneratedNever();

        builder.Property(permission => permission.Code)
            .HasMaxLength(100)
            .IsRequired();

        // Add predefined permissions
        builder.HasData(Permission.GetUsers, Permission.ModifyUser);
    }
}