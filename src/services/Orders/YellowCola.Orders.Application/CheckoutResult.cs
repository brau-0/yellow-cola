using YellowCola.Orders.Domain.Orders;

namespace YellowCola.Orders.Application.Checkout;

public sealed record CheckoutResult(Order Order);