namespace YellowCola.Cart.Domain.Carts;

public sealed class CartItem
{
    internal CartItem(Guid skuId, int quantity)
    {
        if (skuId == Guid.Empty) throw new ArgumentException("SKU id cannot be empty.", nameof(skuId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        SkuId = skuId;
        Quantity = quantity;
    }

    public Guid SkuId { get; }
    public int Quantity { get; private set; }

    internal void Increase(int quantity)
    {
        EnsurePositiveQuantity(quantity);
        Quantity = checked(Quantity + quantity);
    }

    internal void SetQuantity(int quantity)
    {
        EnsurePositiveQuantity(quantity);
        Quantity = quantity;
    }

    private static void EnsurePositiveQuantity(int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
    }
}