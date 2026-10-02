using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using YellowCola.Inventory.Domain.InventoryItems;

namespace YellowCola.Inventory.Infrastructure
    .Persistence.Configurations;

internal sealed class InventoryItemConfiguration
    : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(
        EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable(
            "inventory_items",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_inventory_items_on_hand_non_negative",
                    "\"on_hand\" >= 0");

                table.HasCheckConstraint(
                    "CK_inventory_items_reserved_non_negative",
                    "\"reserved\" >= 0");

                table.HasCheckConstraint(
                    "CK_inventory_items_reserved_not_greater_than_on_hand",
                    "\"reserved\" <= \"on_hand\"");
            });

        builder.HasKey(
            item => item.Id);

        builder.Property(
                item => item.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(
                item => item.SkuId)
            .HasColumnName("sku_id")
            .IsRequired();

        builder.Property(
                item => item.WarehouseCode)
            .HasColumnName("warehouse_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(
                item => item.OnHand)
            .HasColumnName("on_hand")
            .IsRequired();

        builder.Property(
                item => item.Reserved)
            .HasColumnName("reserved")
            .IsRequired();

        builder.Ignore(
            item => item.Available);

        builder.HasIndex(
                item => new
                {
                    item.WarehouseCode,
                    item.SkuId
                })
            .IsUnique()
            .HasDatabaseName(
                "UX_inventory_items_warehouse_code_sku_id");
    }
}