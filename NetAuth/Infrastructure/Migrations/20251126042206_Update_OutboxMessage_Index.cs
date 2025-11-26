using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetAuth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update_OutboxMessage_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_outbox_messages_unprocessed",
                table: "outbox_messages");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_messages_unprocessed",
                table: "outbox_messages",
                columns: new[] { "attempt_count", "occurred_on_utc" },
                filter: "\"processed_on_utc\" IS NULL")
                .Annotation("Npgsql:IndexInclude", new[] { "id", "type", "content" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_outbox_messages_unprocessed",
                table: "outbox_messages");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_messages_unprocessed",
                table: "outbox_messages",
                columns: new[] { "occurred_on_utc", "processed_on_utc" },
                filter: "\"processed_on_utc\" IS NULL")
                .Annotation("Npgsql:IndexInclude", new[] { "id", "type", "content" });
        }
    }
}
