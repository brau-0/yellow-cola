namespace YellowCola.Customer.Domain.Customers;

public sealed class Customer
{
    private readonly List<Address> _addresses = [];

    private Customer()
    {
    }

    public Customer(
        Guid id,
        string externalSubject)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer id cannot be empty.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(externalSubject))
        {
            throw new ArgumentException(
                "External subject is required.",
                nameof(externalSubject));
        }

        Id = id;
        ExternalSubject = externalSubject.Trim();
    }

    public Guid Id { get; private set; }

    public string ExternalSubject { get; private set; } = null!;

    public IReadOnlyCollection<Address> Addresses =>
        _addresses.AsReadOnly();

    public Address AddAddress(
        Guid addressId,
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
        var address = new Address(
            addressId,
            Id,
            label,
            recipientName,
            postalCode,
            state,
            municipality,
            neighborhood,
            street,
            exteriorNumber,
            interiorNumber,
            reference);

        if (_addresses.Count == 0)
        {
            address.SetAsDefault();
        }

        _addresses.Add(address);

        return address;
    }

    public void SetDefaultAddress(Guid addressId)
    {
        var selected =
            _addresses.SingleOrDefault(
                address => address.Id == addressId)
            ?? throw new InvalidOperationException(
                "Address does not belong to this customer.");

        foreach (var address in _addresses)
        {
            address.ClearDefault();
        }

        selected.SetAsDefault();
    }
}
