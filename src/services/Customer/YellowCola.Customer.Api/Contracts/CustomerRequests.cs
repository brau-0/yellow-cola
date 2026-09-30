namespace YellowCola.Customer.Api.Contracts;

public sealed record CreateCustomerRequest(
    string ExternalSubject);

public sealed record CreateAddressRequest(
    string Label,
    string RecipientName,
    string PostalCode,
    string State,
    string Municipality,
    string Neighborhood,
    string Street,
    string ExteriorNumber,
    string? InteriorNumber,
    string? Reference);