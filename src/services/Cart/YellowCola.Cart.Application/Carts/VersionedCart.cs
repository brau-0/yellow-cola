using YellowCola.Cart.Domain.Carts;

namespace YellowCola.Cart.Application.Carts;

public sealed record VersionedCart(ShoppingCart Cart, long Version);