namespace YellowCola.Orders.Domain.Orders;

public sealed class OrderLine
{
    private OrderLine()
    {
    }

    public OrderLine(Guid skuId, string skuCode, string description, decimal unitPrice, int quantity, string currency)
    {
        if (skuId == Guid.Empty) throw new ArgumentException("SKU id cannot be empty.", nameof(skuId));
        if (string.IsNullOrWhiteSpace(skuCode)) throw new ArgumentException("SKU code is required.", nameof(skuCode));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (!currency.Equals("MXN", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Orders currently support MXN only.", nameof(currency));

        SkuId = skuId;
        SkuCode = skuCode.Trim().ToUpperInvariant();
        Description = description.Trim();
        UnitPrice = unitPrice;
        Quantity = quantity;
        Currency = "MXN";
    }

    public Guid SkuId { get; private set; }
    public string SkuCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public string Currency { get; private set; } = "MXN";
    public decimal LineTotal => checked(UnitPrice * Quantity);
}