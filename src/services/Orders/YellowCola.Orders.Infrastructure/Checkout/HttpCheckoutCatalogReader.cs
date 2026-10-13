using System.Net.Http.Json;

using YellowCola.Orders.Application.Checkout;

namespace YellowCola.Orders.Infrastructure.Checkout;

internal sealed class HttpCheckoutCatalogReader(HttpClient httpClient) : ICheckoutCatalogReader
{
    public async Task<IReadOnlyCollection<CheckoutCatalogItem>> GetItemsAsync(IReadOnlyCollection<Guid> skuIds
        , CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(skuIds);

        if (skuIds.Count == 0) return [];

        var ids = skuIds.Distinct().ToArray();
        var query = string.Join("&", ids.Select(id => $"ids={id:D}"));

        using var response = await httpClient.GetAsync($"api/catalog/skus?{query}", cancellationToken);

        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<CatalogSkuApiResponse>>(cancellationToken)
            ?? throw new InvalidOperationException("Catalog API returned an empty response.");

        return items.Select(item => new CheckoutCatalogItem(item.Id, item.Code, item.Description, item.Price
            , item.Currency, item.IsActive)).ToArray();
    }
}