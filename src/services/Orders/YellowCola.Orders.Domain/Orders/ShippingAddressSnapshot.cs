namespace YellowCola.Orders.Domain.Orders;

public sealed class ShippingAddressSnapshot
{
    private ShippingAddressSnapshot()
    {
    }

    public ShippingAddressSnapshot(Guid sourceAddressId, string recipientName, string postalCode, string state, string municipality, string neighborhood, string street, string exteriorNumber, string? interiorNumber, string? reference)
    {
        if (sourceAddressId == Guid.Empty) throw new ArgumentException("Source address id cannot be empty.", nameof(sourceAddressId));

        SourceAddressId = sourceAddressId;
        RecipientName = Required(recipientName, nameof(recipientName));
        PostalCode = ValidatePostalCode(postalCode);
        State = Required(state, nameof(state));
        Municipality = Required(municipality, nameof(municipality));
        Neighborhood = Required(neighborhood, nameof(neighborhood));
        Street = Required(street, nameof(street));
        ExteriorNumber = Required(exteriorNumber, nameof(exteriorNumber));
        InteriorNumber = NormalizeOptional(interiorNumber);
        Reference = NormalizeOptional(reference);
    }

    public Guid SourceAddressId { get; private set; }
    public string RecipientName { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string Municipality { get; private set; } = string.Empty;
    public string Neighborhood { get; private set; } = string.Empty;
    public string Street { get; private set; } = string.Empty;
    public string ExteriorNumber { get; private set; } = string.Empty;
    public string? InteriorNumber { get; private set; }
    public string? Reference { get; private set; }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{parameterName} is required.", parameterName);
        return value.Trim();
    }

    private static string ValidatePostalCode(string postalCode)
    {
        var value = Required(postalCode, nameof(postalCode));
        if (value.Length != 5 || !value.All(char.IsDigit)) throw new ArgumentException("Postal code must contain exactly 5 digits.", nameof(postalCode));
        return value;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}