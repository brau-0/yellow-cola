using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YellowCola.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryInboxAndOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var columns_inbox = new[] { "processed_at_utc", "received_at_utc" };
            var columns_outbox = new[] { "published_at_utc", "created_at_utc" };
            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => x.message_id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    causation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    publish_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.message_id);
                    table.CheckConstraint("CK_outbox_messages_publish_attempts_non_negative", "\"publish_attempts\" >= 0");
                    table.CheckConstraint("CK_outbox_messages_schema_version_positive", "\"schema_version\" > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_correlation_id",
                table: "inbox_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_processed_received",
                table: "inbox_messages",
                columns: columns_inbox);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_correlation_id",
                table: "outbox_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_published_created",
                table: "outbox_messages",
                columns: columns_outbox);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "outbox_messages");
        }
    }
}
