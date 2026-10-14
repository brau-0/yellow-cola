using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YellowCola.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var columns_saga = new[] { "status", "updated_at_utc" };
            migrationBuilder.CreateTable(
                name: "checkout_sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cart_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkout_sagas", x => x.id);
                    table.CheckConstraint("CK_checkout_sagas_status", "\"status\" IN ('Started', 'InventoryReservationPending', 'InventoryReserved', 'PaymentAuthorizationPending', 'PaymentAuthorized', 'InventoryReleasePending', 'Completed', 'Failed')");
                    table.CheckConstraint("CK_checkout_sagas_updated_after_created", "\"updated_at_utc\" >= \"created_at_utc\"");
                    table.ForeignKey(
                        name: "FK_checkout_sagas_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sagas_status_updated_at",
                table: "checkout_sagas",
                columns: columns_saga);

            migrationBuilder.CreateIndex(
                name: "UX_checkout_sagas_order_id",
                table: "checkout_sagas",
                column: "order_id",
                unique: true);
        }



        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkout_sagas");
        }
    }
}
