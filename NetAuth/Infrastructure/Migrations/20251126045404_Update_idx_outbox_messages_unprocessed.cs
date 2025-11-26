using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetAuth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update_idx_outbox_messages_unprocessed : Migration
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
                column: "occurred_on_utc",
                filter: "\"processed_on_utc\" IS NULL")
                .Annotation("Npgsql:IndexInclude", new[] { "id", "type", "content", "attempt_count" });
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
                columns: new[] { "processed_on_utc", "attempt_count", "occurred_on_utc" },
                filter: "\"processed_on_utc\" IS NULL")
                .Annotation("Npgsql:IndexInclude", new[] { "id", "type", "content" });
        }
    }
}
