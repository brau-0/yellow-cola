namespace YellowCola.Cart.Domain.Carts;

public sealed class ShoppingCart
{
    private readonly List<CartItem> _items = [];

    public ShoppingCart(Guid id, Guid? customerId = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Cart id cannot be empty.", nameof(id));
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id cannot be empty.", nameof(customerId));

        Id = id;
        CustomerId = customerId;
    }

    public Guid Id { get; }
    public Guid? CustomerId { get; private set; }
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();
    public int TotalQuantity => _items.Sum(item => item.Quantity);

    public void AssignCustomer(Guid customerId)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id cannot be empty.", nameof(customerId));

        if (CustomerId == customerId) return;
        if (CustomerId.HasValue) throw new InvalidOperationException("Cart is already assigned to another customer.");

        CustomerId = customerId;
    }

    public void AddItem(Guid skuId, int quantity)
    {
        ValidateSkuAndQuantity(skuId, quantity);
        EnsureTotalQuantityDoesNotOverflow(quantity);

        var existingItem = _items.SingleOrDefault(item => item.SkuId == skuId);

        if (existingItem is null)
        {
            _items.Add(new CartItem(skuId, quantity));
            return;
        }

        existingItem.Increase(quantity);
    }
    public void SetQuantity(Guid skuId, int quantity)
    {
        ValidateSkuAndQuantity(skuId, quantity);

        var item = _items.SingleOrDefault(item => item.SkuId == skuId)
            ?? throw new InvalidOperationException("Cart item was not found.");

        var newTotal = _items.Sum(current => (long)current.Quantity) - item.Quantity + quantity;

        if (newTotal > int.MaxValue) throw new InvalidOperationException("Cart total quantity exceeds the supported limit.");

        item.SetQuantity(quantity);
    }

    public bool RemoveItem(Guid skuId)
    {
        if (skuId == Guid.Empty) throw new ArgumentException("SKU id cannot be empty.", nameof(skuId));

        var item = _items.SingleOrDefault(item => item.SkuId == skuId);
        if (item is null) return false;

        _items.Remove(item);
        return true;
    }
    public void MergeAnonymousCart(ShoppingCart source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Id == Id) throw new InvalidOperationException("A cart cannot be merged with itself.");
        if (!CustomerId.HasValue) throw new InvalidOperationException("Target cart must belong to a customer.");
        if (source.CustomerId.HasValue) throw new InvalidOperationException("Source cart must be anonymous.");

        foreach (var item in source.Items) AddItem(item.SkuId, item.Quantity);
    }

    private static void ValidateSkuAndQuantity(Guid skuId, int quantity)
    {
        if (skuId == Guid.Empty) throw new ArgumentException("SKU id cannot be empty.", nameof(skuId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
    }
    private void EnsureTotalQuantityDoesNotOverflow(int quantity)
    {
        var newTotal = _items.Sum(item => (long)item.Quantity) + quantity;

        if (newTotal > int.MaxValue) throw new InvalidOperationException("Cart total quantity exceeds the supported limit.");
    }
}