namespace YellowCola.Customer.Application.Queries;

public sealed record AddressDto(
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

public sealed record CustomerDto(
    Guid Id,
    string ExternalSubject,
    IReadOnlyCollection<AddressDto> Addresses);