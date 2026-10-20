using Microsoft.EntityFrameworkCore;

using YellowCola.Orders.Domain.Checkout;
using YellowCola.Orders.Domain.Orders;
using YellowCola.Orders.Infrastructure.Persistence.Outbox;

namespace YellowCola.Orders.Infrastructure.Persistence;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<CheckoutSaga> CheckoutSagas => Set<CheckoutSaga>();
    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
    }
}