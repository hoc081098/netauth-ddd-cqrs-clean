using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetAuth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_OutboxMessages_IdxOutboxCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "idx_outbox_cleanup",
                table: "outbox_messages",
                columns: new[] { "occurred_on_utc", "id" },
                filter: "\"processed_on_utc\" IS NOT NULL AND \"error\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_outbox_cleanup",
                table: "outbox_messages");
        }
    }
}
