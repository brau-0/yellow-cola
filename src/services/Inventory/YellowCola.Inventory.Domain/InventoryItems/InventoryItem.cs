namespace YellowCola.Inventory.Domain.InventoryItems;

public sealed class InventoryItem
{
    private InventoryItem()
    {
    }

    public InventoryItem(
        Guid id,
        Guid skuId,
        string warehouseCode,
        int onHand,
        int reserved = 0)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Inventory item id cannot be empty.",
                nameof(id));
        }

        if (skuId == Guid.Empty)
        {
            throw new ArgumentException(
                "SKU id cannot be empty.",
                nameof(skuId));
        }

        if (string.IsNullOrWhiteSpace(warehouseCode))
        {
            throw new ArgumentException(
                "Warehouse code is required.",
                nameof(warehouseCode));
        }

        if (onHand < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(onHand),
                "On-hand quantity cannot be negative.");
        }

        if (reserved < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reserved),
                "Reserved quantity cannot be negative.");
        }

        if (reserved > onHand)
        {
            throw new ArgumentException(
                "Reserved quantity cannot exceed on-hand quantity.",
                nameof(reserved));
        }

        Id = id;
        SkuId = skuId;
        WarehouseCode = warehouseCode.Trim();
        OnHand = onHand;
        Reserved = reserved;
    }

    public Guid Id { get; private set; }

    public Guid SkuId { get; private set; }

    public string WarehouseCode { get; private set; } = null!;

    public int OnHand { get; private set; }

    public int Reserved { get; private set; }

    public int Available =>
        OnHand - Reserved;

    public void Reserve(int quantity)
    {
        EnsurePositiveQuantity(quantity);

        if (quantity > Available)
        {
            throw new InvalidOperationException(
                "Insufficient available inventory.");
        }

        Reserved += quantity;
    }

    public void Release(int quantity)
    {
        EnsurePositiveQuantity(quantity);

        if (quantity > Reserved)
        {
            throw new InvalidOperationException(
                "Cannot release more inventory than is currently reserved.");
        }

        Reserved -= quantity;
    }

    public void Receive(int quantity)
    {
        EnsurePositiveQuantity(quantity);

        OnHand += quantity;
    }

    private static void EnsurePositiveQuantity(
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }
    }
    public void ConsumeReserved(
    int quantity)
    {
        EnsurePositiveQuantity(
            quantity);

        if (quantity > Reserved)
        {
            throw new InvalidOperationException(
                "Cannot consume more inventory than is reserved.");
        }

        Reserved -= quantity;
        OnHand -= quantity;
    }
}