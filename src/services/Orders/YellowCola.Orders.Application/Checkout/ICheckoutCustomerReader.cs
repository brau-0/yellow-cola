namespace YellowCola.Orders.Application.Checkout;

public interface ICheckoutCustomerReader
{
    Task<CheckoutAddress?> GetAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default);
}