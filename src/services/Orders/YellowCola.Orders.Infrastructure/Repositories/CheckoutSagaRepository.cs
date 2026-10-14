using Microsoft.EntityFrameworkCore;

using YellowCola.Orders.Application.Checkout;
using YellowCola.Orders.Domain.Checkout;
using YellowCola.Orders.Infrastructure.Persistence;

namespace YellowCola.Orders.Infrastructure.Repositories;

internal sealed class CheckoutSagaRepository(OrdersDbContext dbContext) : ICheckoutSagaRepository
{
    public async Task AddAsync(CheckoutSaga saga, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(saga);
        await dbContext.CheckoutSagas.AddAsync(saga, cancellationToken);
    }

    public Task<CheckoutSaga?> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        if (sagaId == Guid.Empty) throw new ArgumentException("Saga id cannot be empty.", nameof(sagaId));

        return dbContext.CheckoutSagas.SingleOrDefaultAsync(saga => saga.Id == sagaId, cancellationToken);
    }

    public Task<CheckoutSaga?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order id cannot be empty.", nameof(orderId));

        return dbContext.CheckoutSagas.SingleOrDefaultAsync(saga => saga.OrderId == orderId, cancellationToken);
    }
}