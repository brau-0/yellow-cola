using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using YellowCola.Customer.Domain.Customers;

using CustomerEntity =
    YellowCola.Customer.Domain.Customers.Customer;

namespace YellowCola.Customer.Infrastructure
    .Persistence.Configurations;

internal sealed class CustomerConfiguration
    : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(
        EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(customer => customer.ExternalSubject)
            .HasColumnName("external_subject")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(customer => customer.ExternalSubject)
            .IsUnique();

        builder.HasMany(customer => customer.Addresses)
            .WithOne()
            .HasForeignKey(address => address.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(customer => customer.Addresses)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}