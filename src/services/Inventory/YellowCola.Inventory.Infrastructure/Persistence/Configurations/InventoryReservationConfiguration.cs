using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using YellowCola.Inventory.Domain.InventoryItems;
using YellowCola.Inventory.Domain.Reservations;

namespace YellowCola.Inventory.Infrastructure
    .Persistence.Configurations;

internal sealed class InventoryReservationConfiguration
    : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(
        EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable(
            "inventory_reservations",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_inventory_reservations_quantity_positive",
                    "\"quantity\" > 0");

                table.HasCheckConstraint(
                    "CK_inventory_reservations_expiration_after_creation",
                    "\"expires_at_utc\" > \"created_at_utc\"");
            });

        builder.HasKey(
            reservation => reservation.Id);

        builder.Property(
                reservation => reservation.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(
                reservation => reservation.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(
                reservation => reservation.InventoryItemId)
            .HasColumnName("inventory_item_id")
            .IsRequired();

        builder.Property(
                reservation => reservation.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(
                reservation => reservation.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(
                reservation => reservation.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(
                reservation => reservation.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<InventoryItem>()
            .WithMany()
            .HasForeignKey(
                reservation =>
                    reservation.InventoryItemId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasIndex(
                reservation => new
                {
                    reservation.OrderId,
                    reservation.InventoryItemId
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_inventory_reservations_order_inventory_item");

        builder.HasIndex(
                reservation => new
                {
                    reservation.Status,
                    reservation.ExpiresAtUtc
                })
            .HasDatabaseName(
                "IX_inventory_reservations_status_expires_at");
    }
}