namespace YellowCola.Customer.Domain.Customers;

public sealed class Address
{
    private Address()
    {
    }

    internal Address(
        Guid id,
        Guid customerId,
        string label,
        string recipientName,
        string postalCode,
        string state,
        string municipality,
        string neighborhood,
        string street,
        string exteriorNumber,
        string? interiorNumber,
        string? reference)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Address id cannot be empty.",
                nameof(id));
        }

        if (customerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer id cannot be empty.",
                nameof(customerId));
        }

        Id = id;
        CustomerId = customerId;

        Label = Required(label, nameof(label));
        RecipientName =
            Required(recipientName, nameof(recipientName));

        PostalCode = ValidatePostalCode(postalCode);

        State =
            Required(state, nameof(state));

        Municipality =
            Required(municipality, nameof(municipality));

        Neighborhood =
            Required(neighborhood, nameof(neighborhood));

        Street =
            Required(street, nameof(street));

        ExteriorNumber =
            Required(exteriorNumber, nameof(exteriorNumber));

        InteriorNumber =
            NormalizeOptional(interiorNumber);

        Reference =
            NormalizeOptional(reference);
    }
    private static string ValidatePostalCode(
    string postalCode)
    {
        var value =
            Required(
                postalCode,
                nameof(postalCode));

        if (value.Length != 5 ||
            !value.All(char.IsDigit))
        {
            throw new ArgumentException(
                "Postal code must contain exactly 5 digits.",
                nameof(postalCode));
        }

        return value;
    }
    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Label { get; private set; } = null!;

    public string RecipientName { get; private set; } = null!;

    public string PostalCode { get; private set; } = null!;

    public string State { get; private set; } = null!;

    public string Municipality { get; private set; } = null!;

    public string Neighborhood { get; private set; } = null!;

    public string Street { get; private set; } = null!;

    public string ExteriorNumber { get; private set; } = null!;

    public string? InteriorNumber { get; private set; }

    public string? Reference { get; private set; }

    public bool IsDefault { get; private set; }

    internal void SetAsDefault()
    {
        IsDefault = true;
    }

    internal void ClearDefault()
    {
        IsDefault = false;
    }

    private static string Required(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
