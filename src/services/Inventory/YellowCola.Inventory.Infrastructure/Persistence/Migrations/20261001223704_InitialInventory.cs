using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YellowCola.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialInventory : Migration
    {
        private static readonly string[] WarehouseSkuColumns =
            [
            "warehouse_code",
            "sku_id"
            ];
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    on_hand = table.Column<int>(type: "integer", nullable: false),
                    reserved = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_items", x => x.id);
                    table.CheckConstraint("CK_inventory_items_on_hand_non_negative", "\"on_hand\" >= 0");
                    table.CheckConstraint("CK_inventory_items_reserved_non_negative", "\"reserved\" >= 0");
                    table.CheckConstraint("CK_inventory_items_reserved_not_greater_than_on_hand", "\"reserved\" <= \"on_hand\"");
                });

            migrationBuilder.CreateIndex(
                name: "UX_inventory_items_warehouse_code_sku_id",
                table: "inventory_items",
                columns: WarehouseSkuColumns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_items");
        }
    }
}
