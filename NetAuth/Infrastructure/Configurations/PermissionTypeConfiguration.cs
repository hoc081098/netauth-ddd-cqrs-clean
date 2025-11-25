using System.Globalization;
using EFCore.NamingConventions.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Configurations;

public class PermissionTypeConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        var snakeCaseNameRewriter = new SnakeCaseNameRewriter(CultureInfo.InvariantCulture);

        builder.ToTable(snakeCaseNameRewriter.RewriteName(nameof(Permission)));

        builder.HasKey(permission => permission.Code);

        builder.Property(permission => permission.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasData(Permission.GetUsers, Permission.ModifyUser);
    }
}
