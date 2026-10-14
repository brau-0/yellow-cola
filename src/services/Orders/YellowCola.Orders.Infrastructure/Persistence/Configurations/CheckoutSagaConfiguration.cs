using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using YellowCola.Orders.Domain.Checkout;
using YellowCola.Orders.Domain.Orders;

namespace YellowCola.Orders.Infrastructure.Persistence.Configurations;

internal sealed class CheckoutSagaConfiguration : IEntityTypeConfiguration<CheckoutSaga>
{
    public void Configure(EntityTypeBuilder<CheckoutSaga> builder)
    {
        builder.ToTable("checkout_sagas", table =>
        {
            table.HasCheckConstraint(
                "CK_checkout_sagas_status",
                "\"status\" IN ('Started', 'InventoryReservationPending', 'InventoryReserved', 'PaymentAuthorizationPending', 'PaymentAuthorized', 'InventoryReleasePending', 'Completed', 'Failed')");

            table.HasCheckConstraint(
                "CK_checkout_sagas_updated_after_created",
                "\"updated_at_utc\" >= \"created_at_utc\"");
        });

        builder.HasKey(saga => saga.Id);

        builder.Property(saga => saga.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(saga => saga.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(saga => saga.CartId).HasColumnName("cart_id").IsRequired();
        builder.Property(saga => saga.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(saga => saga.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(saga => saga.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(saga => saga.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(saga => saga.FailureReason).HasColumnName("failure_reason").HasMaxLength(1000);

        builder.HasIndex(saga => saga.OrderId).IsUnique().HasDatabaseName("UX_checkout_sagas_order_id");
        builder.HasIndex(saga => new { saga.Status, saga.UpdatedAtUtc }).HasDatabaseName("IX_checkout_sagas_status_updated_at");

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(saga => saga.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}