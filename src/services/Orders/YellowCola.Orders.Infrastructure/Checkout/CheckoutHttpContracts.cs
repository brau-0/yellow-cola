namespace YellowCola.Orders.Infrastructure.Checkout;

public sealed record CartApiResponse(
    Guid Id,
    Guid? CustomerId,
    IReadOnlyCollection<CartItemApiResponse> Items,
    int TotalQuantity);

public sealed record CartItemApiResponse(
    Guid SkuId,
    int Quantity);

public sealed record CatalogSkuApiResponse(
    Guid Id,
    string Code,
    string Description,
    decimal Price,
    string Currency,
    bool IsActive);

public sealed record CustomerApiResponse(
    Guid Id,
    string ExternalSubject,
    IReadOnlyCollection<AddressApiResponse> Addresses);

public sealed record AddressApiResponse(
    Guid Id,
    string Label,
    string RecipientName,
    string PostalCode,
    string State,
    string Municipality,
    string Neighborhood,
    string Street,
    string ExteriorNumber,
    string? InteriorNumber,
    string? Reference,
    bool IsDefault);