using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetAuth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_AttemptCount_To_OutboxMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attempt_count",
                table: "outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attempt_count",
                table: "outbox_messages");
        }
    }
}
