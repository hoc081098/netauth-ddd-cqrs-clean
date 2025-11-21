using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetAuth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserTable_EmailColumn_AddUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_user_email",
                table: "user",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_user_email",
                table: "user");
        }
    }
}
