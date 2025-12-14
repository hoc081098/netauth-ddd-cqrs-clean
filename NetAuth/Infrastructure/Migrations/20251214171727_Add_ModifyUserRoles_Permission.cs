using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetAuth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_ModifyUserRoles_Permission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "id", "code" },
                values: new object[] { 8, "users:roles:update" });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "permission_id", "role_id" },
                values: new object[] { 8, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 8, 1 });

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: 8);
        }
    }
}
