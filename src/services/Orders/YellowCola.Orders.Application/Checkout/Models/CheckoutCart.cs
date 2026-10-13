namespace YellowCola.Orders.Application.Checkout;

public sealed record CheckoutCart(Guid Id, Guid? CustomerId, IReadOnlyCollection<CheckoutCartItem> Items);

public sealed record CheckoutCartItem(Guid SkuId, int Quantity);