using Microsoft.EntityFrameworkCore;

using YellowCola.Customer.Domain.Customers;

using CustomerEntity =
    YellowCola.Customer.Domain.Customers.Customer;

namespace YellowCola.Customer.Infrastructure.Persistence;

public sealed class CustomerDbContext(
    DbContextOptions<CustomerDbContext> options)
    : DbContext(options)
{
    public DbSet<CustomerEntity> Customers =>
        Set<CustomerEntity>();

    public DbSet<Address> Addresses =>
        Set<Address>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CustomerDbContext).Assembly);
    }
}