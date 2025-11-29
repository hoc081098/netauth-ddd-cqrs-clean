using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetAuth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAppDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_role_permission_permissions_permission_id",
                table: "role_permission");

            migrationBuilder.DropForeignKey(
                name: "fk_role_permission_roles_role_id",
                table: "role_permission");

            migrationBuilder.DropForeignKey(
                name: "fk_role_user_roles_role_id",
                table: "role_user");

            migrationBuilder.DropForeignKey(
                name: "fk_role_user_users_user_id",
                table: "role_user");

            migrationBuilder.DropPrimaryKey(
                name: "pk_role_user",
                table: "role_user");

            migrationBuilder.DropPrimaryKey(
                name: "pk_role_permission",
                table: "role_permission");

            migrationBuilder.RenameTable(
                name: "role_user",
                newName: "role_users");

            migrationBuilder.RenameTable(
                name: "role_permission",
                newName: "role_permissions");

            migrationBuilder.RenameIndex(
                name: "ix_role_user_user_id",
                table: "role_users",
                newName: "ix_role_users_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_role_permission_role_id",
                table: "role_permissions",
                newName: "ix_role_permissions_role_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_role_users",
                table: "role_users",
                columns: new[] { "role_id", "user_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_role_permissions",
                table: "role_permissions",
                columns: new[] { "permission_id", "role_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_role_permissions_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id",
                principalTable: "permissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_role_permissions_roles_role_id",
                table: "role_permissions",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_role_users_roles_role_id",
                table: "role_users",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_role_users_users_user_id",
                table: "role_users",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_role_permissions_permissions_permission_id",
                table: "role_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_role_permissions_roles_role_id",
                table: "role_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_role_users_roles_role_id",
                table: "role_users");

            migrationBuilder.DropForeignKey(
                name: "fk_role_users_users_user_id",
                table: "role_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_role_users",
                table: "role_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_role_permissions",
                table: "role_permissions");

            migrationBuilder.RenameTable(
                name: "role_users",
                newName: "role_user");

            migrationBuilder.RenameTable(
                name: "role_permissions",
                newName: "role_permission");

            migrationBuilder.RenameIndex(
                name: "ix_role_users_user_id",
                table: "role_user",
                newName: "ix_role_user_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_role_permissions_role_id",
                table: "role_permission",
                newName: "ix_role_permission_role_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_role_user",
                table: "role_user",
                columns: new[] { "role_id", "user_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_role_permission",
                table: "role_permission",
                columns: new[] { "permission_id", "role_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_role_permission_permissions_permission_id",
                table: "role_permission",
                column: "permission_id",
                principalTable: "permissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_role_permission_roles_role_id",
                table: "role_permission",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_role_user_roles_role_id",
                table: "role_user",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_role_user_users_user_id",
                table: "role_user",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
