using YellowCola.Cart.Domain.Carts;

namespace YellowCola.Cart.Api.Contracts;

internal static class CartMappings
{
    public static CartResponse ToResponse(this ShoppingCart cart)
    {
        var items = cart.Items.Select(item => new CartItemResponse(item.SkuId, item.Quantity)).ToArray();
        return new CartResponse(cart.Id, cart.CustomerId, items, cart.TotalQuantity);
    }
}