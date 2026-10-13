namespace YellowCola.Orders.Application.Checkout;

public sealed record CheckoutCatalogItem(Guid SkuId, string SkuCode, string Description, decimal UnitPrice, string Currency, bool IsActive);