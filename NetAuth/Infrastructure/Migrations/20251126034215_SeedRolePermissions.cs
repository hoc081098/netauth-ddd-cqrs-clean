using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NetAuth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "role_permission",
                columns: new[] { "permission_id", "role_id" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 2 },
                    { 2, 1 },
                    { 2, 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 1, 2 });

            migrationBuilder.DeleteData(
                table: "role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 2, 1 });

            migrationBuilder.DeleteData(
                table: "role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 2, 2 });
        }
    }
}
