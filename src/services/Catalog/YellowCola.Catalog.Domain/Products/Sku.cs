namespace YellowCola.Catalog.Domain.Products;

public sealed class Sku
{
    private Sku()
    {
    }

    public Sku(
    Guid id,
    Guid productId,
    string code,
    string flavor,
    int unitSizeMl,
    string packageType,
    int unitsPerPack,
    string packLabel,
    decimal price,
    string currency = "MXN")
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "SKU id cannot be empty.",
                nameof(id));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product id cannot be empty.",
                nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "SKU code is required.",
                nameof(code));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(unitSizeMl);

        if (string.IsNullOrWhiteSpace(packageType))
        {
            throw new ArgumentException(
                "Package type is required.",
                nameof(packageType));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(unitsPerPack);

        ArgumentOutOfRangeException.ThrowIfNegative(price);

        if (!currency.Equals(
                "MXN",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Catalog v1 supports MXN only.",
                nameof(currency));
        }

        Id = id;
        ProductId = productId;
        Code = code.Trim().ToUpperInvariant();
        Flavor = flavor.Trim().ToUpperInvariant();
        UnitSizeMl = unitSizeMl;
        PackageType = packageType.Trim().ToUpperInvariant();
        UnitsPerPack = unitsPerPack;
        PackLabel = packLabel.Trim();
        Price = price;
        Currency = "MXN";
        IsActive = true;
    }
    public string Flavor { get; private set; } = string.Empty;

    public string PackLabel { get; private set; } = string.Empty;

    public string Currency { get; private set; } = "MXN";
    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public int UnitSizeMl { get; private set; }

    public string PackageType { get; private set; } = string.Empty;

    public int UnitsPerPack { get; private set; }

    public decimal Price { get; private set; }

    public bool IsActive { get; private set; }

    public void ChangePrice(decimal newPrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(newPrice);

        Price = newPrice;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}