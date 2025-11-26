using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetAuth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConfiguration01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_role_users",
                table: "role_users");

            migrationBuilder.DropIndex(
                name: "ix_role_users_role_id",
                table: "role_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_role_permissions",
                table: "role_permissions");

            migrationBuilder.DropIndex(
                name: "ix_role_permissions_permission_id",
                table: "role_permissions");

            migrationBuilder.AddPrimaryKey(
                name: "pk_role_users",
                table: "role_users",
                columns: new[] { "role_id", "user_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_role_permissions",
                table: "role_permissions",
                columns: new[] { "permission_id", "role_id" });

            migrationBuilder.CreateIndex(
                name: "ix_role_users_user_id",
                table: "role_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_role_id",
                table: "role_permissions",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_role_users",
                table: "role_users");

            migrationBuilder.DropIndex(
                name: "ix_role_users_user_id",
                table: "role_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_role_permissions",
                table: "role_permissions");

            migrationBuilder.DropIndex(
                name: "ix_role_permissions_role_id",
                table: "role_permissions");

            migrationBuilder.AddPrimaryKey(
                name: "pk_role_users",
                table: "role_users",
                columns: new[] { "user_id", "role_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_role_permissions",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" });

            migrationBuilder.CreateIndex(
                name: "ix_role_users_role_id",
                table: "role_users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");
        }
    }
}
