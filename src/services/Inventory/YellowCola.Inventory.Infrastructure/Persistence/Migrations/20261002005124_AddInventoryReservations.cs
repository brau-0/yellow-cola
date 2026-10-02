using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YellowCola.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryReservations : Migration
    {
        private static readonly string[] OrderInventoryItemColumns = [
                "order_id",
                "inventory_item_id"
                ];

        private static readonly string[] StatusExpiresAtColumns =
            [
            "status",
            "expires_at_utc"            
            ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_reservations", x => x.id);
                    table.CheckConstraint("CK_inventory_reservations_expiration_after_creation", "\"expires_at_utc\" > \"created_at_utc\"");
                    table.CheckConstraint("CK_inventory_reservations_quantity_positive", "\"quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_inventory_reservations_inventory_items_inventory_item_id",
                        column: x => x.inventory_item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_inventory_item_id",
                table: "inventory_reservations",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_status_expires_at",
                table: "inventory_reservations",
                columns: StatusExpiresAtColumns);

            migrationBuilder.CreateIndex(
                name: "UX_inventory_reservations_order_inventory_item",
                table: "inventory_reservations",
                columns: OrderInventoryItemColumns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_reservations");
        }
    }
}
