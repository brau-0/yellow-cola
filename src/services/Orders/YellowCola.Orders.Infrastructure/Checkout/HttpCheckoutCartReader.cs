using System.Net;
using System.Net.Http.Json;

using YellowCola.Orders.Application.Checkout;

namespace YellowCola.Orders.Infrastructure.Checkout;

public sealed class HttpCheckoutCartReader(HttpClient httpClient) : ICheckoutCartReader
{
    public async Task<CheckoutCart?> GetAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        if (cartId == Guid.Empty) throw new ArgumentException("Cart id cannot be empty.", nameof(cartId));

        using var response = await httpClient.GetAsync($"api/carts/{cartId:D}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();

        var cart = await response.Content.ReadFromJsonAsync<CartApiResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Cart API returned an empty response.");

        var items = cart.Items.Select(item => new CheckoutCartItem(item.SkuId, item.Quantity)).ToArray();

        return new CheckoutCart(cart.Id, cart.CustomerId, items);
    }
}
