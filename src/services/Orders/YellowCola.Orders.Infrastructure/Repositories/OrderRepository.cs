using Microsoft.EntityFrameworkCore;

using YellowCola.Orders.Application.Orders;
using YellowCola.Orders.Domain.Orders;
using YellowCola.Orders.Infrastructure.Persistence;

namespace YellowCola.Orders.Infrastructure.Repositories;

internal sealed class OrderRepository(OrdersDbContext dbContext) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        await dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order id cannot be empty.", nameof(orderId));

        return dbContext.Orders.Include(order => order.Lines).SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }
}