namespace YellowCola.Catalog.Domain.Products;

public sealed class Product
{
    public string Code { get; private set; } = string.Empty;
    public string ShortDescription { get; private set; } = string.Empty;
    public string PrimaryImageKey { get; private set; } = string.Empty;
    private readonly List<Sku> _skus = [];

    private Product()
    {
    }

    public Product(
    Guid id,
    string code,
    Guid categoryId,
    string brand,
    string name,
    string slug,
    string shortDescription,
    string description,
    string primaryImageKey)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Product id cannot be empty.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Product code is required.",
                nameof(code));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Category id cannot be empty.",
                nameof(categoryId));
        }

        if (string.IsNullOrWhiteSpace(brand))
        {
            throw new ArgumentException(
                "Brand is required.",
                nameof(brand));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Product name is required.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException(
                "Product slug is required.",
                nameof(slug));
        }

        Id = id;
        Code = code.Trim().ToUpperInvariant();
        CategoryId = categoryId;
        Brand = brand.Trim();
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        ShortDescription = shortDescription.Trim();
        Description = description.Trim();
        PrimaryImageKey = primaryImageKey.Trim();
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public Guid CategoryId { get; private set; }

    public string Brand { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<Sku> Skus => _skus.AsReadOnly();

    public void AddSku(Sku sku)
    {
        ArgumentNullException.ThrowIfNull(sku);

        if (sku.ProductId != Id)
        {
            throw new InvalidOperationException(
                "The SKU belongs to a different product.");
        }

        if (_skus.Any(x =>
                x.Code.Equals(
                    sku.Code,
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"SKU '{sku.Code}' already exists in this product.");
        }

        _skus.Add(sku);
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