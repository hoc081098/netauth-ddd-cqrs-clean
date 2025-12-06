using System.Globalization;
using EFCore.NamingConventions.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetAuth.Domain.Users;
using NetAuth.Infrastructure.Models;

namespace NetAuth.Infrastructure.Configurations;

public class RoleTypeConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(
                id => id.Value,
                value => new RoleId(value)
            )
            .ValueGeneratedNever();

        builder.Property(r => r.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasMany(role => role.Permissions)
            .WithMany()
            .UsingEntity<RolePermission>(
                configureRight: rolePermissionBuilder =>
                    rolePermissionBuilder.HasOne<Permission>()
                        .WithMany()
                        .HasForeignKey(rolePermission => rolePermission.PermissionId),
                configureLeft: rolePermissionBuilder =>
                    rolePermissionBuilder.HasOne<Role>()
                        .WithMany()
                        .HasForeignKey(rolePermission => rolePermission.RoleId),
                configureJoinEntityType: rolePermissionBuilder =>
                {
                    rolePermissionBuilder.Property(rolePermission => rolePermission.RoleId)
                        .HasConversion(
                            id => id.Value,
                            value => new RoleId(value));

                    rolePermissionBuilder.Property(rolePermission => rolePermission.PermissionId)
                        .HasConversion(
                            id => id.Value,
                            value => new PermissionId(value));

                    rolePermissionBuilder.HasData(
                        // Member role permissions
                        CreateRolePermission(Role.Member, Permission.GetUsers),
                        CreateRolePermission(Role.Member, Permission.ModifyUser),
                        CreateRolePermission(Role.Member, Permission.GetTodoItems),
                        CreateRolePermission(Role.Member, Permission.CreateTodoItem),

                        // Administrator role permissions
                        CreateRolePermission(Role.Administrator, Permission.GetUsers),
                        CreateRolePermission(Role.Administrator, Permission.ModifyUser),
                        CreateRolePermission(Role.Administrator, Permission.GetTodoItems),
                        CreateRolePermission(Role.Administrator, Permission.CreateTodoItem)
                    );
                }
            );

        // Add predefined roles
        builder.HasData(Role.Administrator, Role.Member);
    }

    private static RolePermission CreateRolePermission(Role role, Permission permission) =>
        new() { RoleId = role.Id, PermissionId = permission.Id };
}