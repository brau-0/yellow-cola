namespace YellowCola.Orders.Application.Checkout;

public interface ICheckoutCartReader
{
    Task<CheckoutCart?> GetAsync(Guid cartId, CancellationToken cancellationToken = default);
}