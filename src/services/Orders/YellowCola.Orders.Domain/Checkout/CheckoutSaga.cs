namespace YellowCola.Orders.Domain.Checkout;

public sealed class CheckoutSaga
{
    private CheckoutSaga()
    {
    }

    public CheckoutSaga(Guid id, Guid orderId, Guid cartId, Guid customerId, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Saga id cannot be empty.", nameof(id));
        if (orderId == Guid.Empty) throw new ArgumentException("Order id cannot be empty.", nameof(orderId));
        if (cartId == Guid.Empty) throw new ArgumentException("Cart id cannot be empty.", nameof(cartId));
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id cannot be empty.", nameof(customerId));
        if (createdAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("CreatedAtUtc must be UTC.", nameof(createdAtUtc));

        Id = id;
        OrderId = orderId;
        CartId = cartId;
        CustomerId = customerId;
        Status = CheckoutSagaStatus.Started;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid CartId { get; private set; }
    public Guid CustomerId { get; private set; }
    public CheckoutSagaStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    public void MarkInventoryReservationPending(DateTimeOffset occurredAtUtc)
    {
        EnsureStatus(CheckoutSagaStatus.Started);
        MoveTo(CheckoutSagaStatus.InventoryReservationPending, occurredAtUtc);
    }

    public void MarkInventoryReserved(DateTimeOffset occurredAtUtc)
    {
        EnsureStatus(CheckoutSagaStatus.InventoryReservationPending);
        MoveTo(CheckoutSagaStatus.InventoryReserved, occurredAtUtc);
    }

    public void MarkPaymentAuthorizationPending(DateTimeOffset occurredAtUtc)
    {
        EnsureStatus(CheckoutSagaStatus.InventoryReserved);
        MoveTo(CheckoutSagaStatus.PaymentAuthorizationPending, occurredAtUtc);
    }

    public void MarkPaymentAuthorized(DateTimeOffset occurredAtUtc)
    {
        EnsureStatus(CheckoutSagaStatus.PaymentAuthorizationPending);
        MoveTo(CheckoutSagaStatus.PaymentAuthorized, occurredAtUtc);
    }

    public void Complete(DateTimeOffset occurredAtUtc)
    {
        EnsureStatus(CheckoutSagaStatus.PaymentAuthorized);
        MoveTo(CheckoutSagaStatus.Completed, occurredAtUtc);
    }

    public void MarkInventoryReleasePending(string reason, DateTimeOffset occurredAtUtc)
    {
        EnsureStatus(CheckoutSagaStatus.PaymentAuthorizationPending);

        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Failure reason is required.", nameof(reason));

        FailureReason = reason.Trim();
        MoveTo(CheckoutSagaStatus.InventoryReleasePending, occurredAtUtc);
    }

    public void Fail(string reason, DateTimeOffset occurredAtUtc)
    {
        if (Status != CheckoutSagaStatus.InventoryReleasePending) throw new InvalidOperationException($"Saga cannot fail from status '{Status}'.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Failure reason is required.", nameof(reason));

        FailureReason = reason.Trim();
        MoveTo(CheckoutSagaStatus.Failed, occurredAtUtc);
    }

    private void EnsureStatus(CheckoutSagaStatus expected)
    {
        if (Status != expected) throw new InvalidOperationException($"Saga expected status '{expected}' but was '{Status}'.");
    }

    private void MoveTo(CheckoutSagaStatus status, DateTimeOffset occurredAtUtc)
    {
        if (occurredAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("OccurredAtUtc must be UTC.", nameof(occurredAtUtc));
        if (occurredAtUtc < UpdatedAtUtc) throw new ArgumentException("OccurredAtUtc cannot be earlier than the previous transition.", nameof(occurredAtUtc));

        Status = status;
        UpdatedAtUtc = occurredAtUtc;
    }
}