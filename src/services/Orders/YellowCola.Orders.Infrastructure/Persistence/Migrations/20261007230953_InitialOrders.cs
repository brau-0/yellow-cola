using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YellowCola.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrders : Migration
    {
        private static readonly string[] CustomerInventoryItemColumns = [
              "customer_id",
                "created_at_utc"
              ];

        private static readonly string[] StatusExpiresAtColumns =
            [
            "status",
            "created_at_utc"
            ];
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipping_source_address_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipping_recipient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    shipping_postal_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    shipping_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    shipping_municipality = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    shipping_neighborhood = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    shipping_street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    shipping_exterior_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    shipping_interior_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    shipping_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.CheckConstraint("CK_orders_status", "\"status\" IN ('Pending', 'Confirmed', 'Cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "order_lines",
                columns: table => new
                {
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_lines", x => new { x.order_id, x.sku_id });
                    table.CheckConstraint("CK_order_lines_currency_mxn", "\"currency\" = 'MXN'");
                    table.CheckConstraint("CK_order_lines_quantity_positive", "\"quantity\" > 0");
                    table.CheckConstraint("CK_order_lines_unit_price_non_negative", "\"unit_price\" >= 0");
                    table.ForeignKey(
                        name: "FK_order_lines_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_orders_customer_created_at",
                table: "orders",
                columns: CustomerInventoryItemColumns);

            migrationBuilder.CreateIndex(
                name: "IX_orders_status_created_at",
                table: "orders",
                columns: StatusExpiresAtColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_lines");

            migrationBuilder.DropTable(
                name: "orders");
        }
    }
}
