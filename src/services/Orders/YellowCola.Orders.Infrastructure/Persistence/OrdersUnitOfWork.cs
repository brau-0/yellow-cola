using YellowCola.Orders.Application.Persistence;

namespace YellowCola.Orders.Infrastructure.Persistence;

internal sealed class OrdersUnitOfWork(OrdersDbContext dbContext) : IOrdersUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}