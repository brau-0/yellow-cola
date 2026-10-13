namespace YellowCola.Orders.Domain.Orders;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    private Order()
    {
    }

    public Order(Guid id, Guid customerId, ShippingAddressSnapshot shippingAddress, IEnumerable<OrderLine> lines, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Order id cannot be empty.", nameof(id));
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id cannot be empty.", nameof(customerId));

        ArgumentNullException.ThrowIfNull(shippingAddress);
        ArgumentNullException.ThrowIfNull(lines);

        if (createdAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("CreatedAtUtc must be UTC.", nameof(createdAtUtc));

        var orderLines = lines.ToList();

        if (orderLines.Count == 0) throw new ArgumentException("Order must contain at least one line.", nameof(lines));
        if (orderLines.Select(line => line.Currency).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1) throw new ArgumentException("All order lines must use the same currency.", nameof(lines));
        if (orderLines.GroupBy(line => line.SkuId).Any(group => group.Count() > 1)) throw new ArgumentException("Order cannot contain duplicate SKU lines.", nameof(lines));

        Id = id;
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        CreatedAtUtc = createdAtUtc;
        Status = OrderStatus.Pending;

        _lines.AddRange(orderLines);
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public ShippingAddressSnapshot ShippingAddress { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
    public string Currency => _lines[0].Currency;
    public decimal TotalAmount => _lines.Sum(line => line.LineTotal);

    public void Confirm()
    {
        if (Status == OrderStatus.Confirmed) return;
        if (Status != OrderStatus.Pending) throw new InvalidOperationException($"Order cannot be confirmed from status '{Status}'.");

        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled) return;
        if (Status != OrderStatus.Pending) throw new InvalidOperationException($"Order cannot be cancelled from status '{Status}'.");

        Status = OrderStatus.Cancelled;
    }
}