namespace YellowCola.Orders.Api.Contracts;

public sealed record CheckoutOrderRequest(Guid CartId, Guid CustomerId, Guid AddressId);

public sealed record OrderLineResponse(
    Guid SkuId,
    string SkuCode,
    string Description,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string Currency);

public sealed record ShippingAddressResponse(
    Guid SourceAddressId,
    string RecipientName,
    string PostalCode,
    string State,
    string Municipality,
    string Neighborhood,
    string Street,
    string ExteriorNumber,
    string? InteriorNumber,
    string? Reference);

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    decimal TotalAmount,
    string Currency,
    ShippingAddressResponse ShippingAddress,
    IReadOnlyCollection<OrderLineResponse> Lines);