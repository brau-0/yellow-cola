using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


using YellowCola.Orders.Domain.Orders;

namespace YellowCola.Orders.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", table =>
        {
            table.HasCheckConstraint("CK_orders_status", "\"status\" IN ('Pending', 'Confirmed', 'Cancelled')");
        });

        builder.HasKey(order => order.Id);

        builder.Property(order => order.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(order => order.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(order => order.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(order => order.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();

        builder.Ignore(order => order.Currency);
        builder.Ignore(order => order.TotalAmount);

        builder.OwnsOne(order => order.ShippingAddress, address =>
        {
            address.Property(value => value.SourceAddressId).HasColumnName("shipping_source_address_id").IsRequired();
            address.Property(value => value.RecipientName).HasColumnName("shipping_recipient_name").HasMaxLength(200).IsRequired();
            address.Property(value => value.PostalCode).HasColumnName("shipping_postal_code").HasMaxLength(5).IsRequired();
            address.Property(value => value.State).HasColumnName("shipping_state").HasMaxLength(100).IsRequired();
            address.Property(value => value.Municipality).HasColumnName("shipping_municipality").HasMaxLength(150).IsRequired();
            address.Property(value => value.Neighborhood).HasColumnName("shipping_neighborhood").HasMaxLength(150).IsRequired();
            address.Property(value => value.Street).HasColumnName("shipping_street").HasMaxLength(200).IsRequired();
            address.Property(value => value.ExteriorNumber).HasColumnName("shipping_exterior_number").HasMaxLength(30).IsRequired();
            address.Property(value => value.InteriorNumber).HasColumnName("shipping_interior_number").HasMaxLength(30);
            address.Property(value => value.Reference).HasColumnName("shipping_reference").HasMaxLength(500);
        });

        builder.Navigation(order => order.ShippingAddress).IsRequired();

        builder.OwnsMany(order => order.Lines, line =>
        {
            line.ToTable("order_lines", table =>
            {
                table.HasCheckConstraint("CK_order_lines_quantity_positive", "\"quantity\" > 0");
                table.HasCheckConstraint("CK_order_lines_unit_price_non_negative", "\"unit_price\" >= 0");
                table.HasCheckConstraint("CK_order_lines_currency_mxn", "\"currency\" = 'MXN'");
            });

            line.WithOwner().HasForeignKey("OrderId");
            line.Property<Guid>("OrderId").HasColumnName("order_id");

            line.HasKey("OrderId", nameof(OrderLine.SkuId));

            line.Property(value => value.SkuId).HasColumnName("sku_id").ValueGeneratedNever();
            line.Property(value => value.SkuCode).HasColumnName("sku_code").HasMaxLength(100).IsRequired();
            line.Property(value => value.Description).HasColumnName("description").HasMaxLength(250).IsRequired();
            line.Property(value => value.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2).IsRequired();
            line.Property(value => value.Quantity).HasColumnName("quantity").IsRequired();
            line.Property(value => value.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();

            line.Ignore(value => value.LineTotal);
        });

        builder.Navigation(order => order.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(order => new { order.CustomerId, order.CreatedAtUtc }).HasDatabaseName("IX_orders_customer_created_at");
        builder.HasIndex(order => new { order.Status, order.CreatedAtUtc }).HasDatabaseName("IX_orders_status_created_at");
    }
}