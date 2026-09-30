using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using YellowCola.Customer.Domain.Customers;

namespace YellowCola.Customer.Infrastructure
    .Persistence.Configurations;

internal sealed class AddressConfiguration
    : IEntityTypeConfiguration<Address>
{
    public void Configure(
        EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");

        builder.HasKey(address => address.Id);

        builder.Property(address => address.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(address => address.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(address => address.Label)
            .HasColumnName("label")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(address => address.RecipientName)
            .HasColumnName("recipient_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(address => address.PostalCode)
            .HasColumnName("postal_code")
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(address => address.State)
            .HasColumnName("state")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(address => address.Municipality)
            .HasColumnName("municipality")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(address => address.Neighborhood)
            .HasColumnName("neighborhood")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(address => address.Street)
            .HasColumnName("street")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(address => address.ExteriorNumber)
            .HasColumnName("exterior_number")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(address => address.InteriorNumber)
            .HasColumnName("interior_number")
            .HasMaxLength(30);

        builder.Property(address => address.Reference)
            .HasColumnName("reference")
            .HasMaxLength(300);

        builder.Property(address => address.IsDefault)
            .HasColumnName("is_default")
            .IsRequired();

        builder.HasIndex(address => address.CustomerId);

        builder.HasIndex(address => address.CustomerId)
            .IsUnique()
            .HasFilter("\"is_default\" = TRUE");
    }
}