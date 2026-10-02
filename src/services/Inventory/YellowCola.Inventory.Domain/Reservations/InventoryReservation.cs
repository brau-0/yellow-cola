namespace YellowCola.Inventory.Domain.Reservations;

public sealed class InventoryReservation
{
    private InventoryReservation()
    {
    }

    public InventoryReservation(
        Guid id,
        Guid orderId,
        Guid inventoryItemId,
        int quantity,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Reservation id cannot be empty.",
                nameof(id));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id cannot be empty.",
                nameof(orderId));
        }

        if (inventoryItemId == Guid.Empty)
        {
            throw new ArgumentException(
                "Inventory item id cannot be empty.",
                nameof(inventoryItemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Reservation quantity must be greater than zero.");
        }

        createdAtUtc =
            createdAtUtc.ToUniversalTime();

        expiresAtUtc =
            expiresAtUtc.ToUniversalTime();

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "Expiration time must be later than creation time.",
                nameof(expiresAtUtc));
        }

        Id = id;
        OrderId = orderId;
        InventoryItemId = inventoryItemId;
        Quantity = quantity;
        Status = InventoryReservationStatus.Active;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid InventoryItemId { get; private set; }

    public int Quantity { get; private set; }

    public InventoryReservationStatus Status
    {
        get;
        private set;
    }

    public DateTimeOffset CreatedAtUtc
    {
        get;
        private set;
    }

    public DateTimeOffset ExpiresAtUtc
    {
        get;
        private set;
    }

    public bool IsActive =>
        Status == InventoryReservationStatus.Active;

    public void Release()
    {
        EnsureActive();

        Status =
            InventoryReservationStatus.Released;
    }

    public void Consume()
    {
        EnsureActive();

        Status =
            InventoryReservationStatus.Consumed;
    }

    public void Expire(
        DateTimeOffset nowUtc)
    {
        EnsureActive();

        nowUtc =
            nowUtc.ToUniversalTime();

        if (nowUtc < ExpiresAtUtc)
        {
            throw new InvalidOperationException(
                "Reservation has not expired yet.");
        }

        Status =
            InventoryReservationStatus.Expired;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException(
                $"Reservation is already {Status}.");
        }
    }
}