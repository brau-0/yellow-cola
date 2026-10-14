using YellowCola.Orders.Domain.Checkout;

namespace YellowCola.Orders.Application.Checkout;

public interface ICheckoutSagaRepository
{
    Task AddAsync(CheckoutSaga saga, CancellationToken cancellationToken = default);
    Task<CheckoutSaga?> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken = default);
    Task<CheckoutSaga?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}