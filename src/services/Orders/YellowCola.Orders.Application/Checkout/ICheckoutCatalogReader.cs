namespace YellowCola.Orders.Application.Checkout;

public interface ICheckoutCatalogReader
{
    Task<IReadOnlyCollection<CheckoutCatalogItem>> GetItemsAsync(IReadOnlyCollection<Guid> skuIds, CancellationToken cancellationToken = default);
}