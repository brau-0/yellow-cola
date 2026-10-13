namespace YellowCola.Orders.Application.Checkout;

public sealed record CheckoutRequest(Guid CartId, Guid CustomerId, Guid AddressId);