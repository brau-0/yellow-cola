namespace YellowCola.Catalog.Infrastructure.Seed;

internal sealed class CatalogSeedFile
{
    public required IReadOnlyCollection<CategorySeedItem> Categories
    {
        get;
        init;
    }

    public required IReadOnlyCollection<ProductSeedItem> Products
    {
        get;
        init;
    }
}

internal sealed class CategorySeedItem
{
    public required string Name { get; init; }

    public required string Slug { get; init; }

    public string? ParentSlug { get; init; }
}

internal sealed class ProductSeedItem
{
    public required string ProductId { get; init; }

    public required string Brand { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public required string CategoryLevel1 { get; init; }

    public required string CategoryLevel2 { get; init; }

    public required string ShortDescription { get; init; }

    public required string Description { get; init; }

    public required string PrimaryImageKey { get; init; }

    public required IReadOnlyCollection<SkuSeedItem> Skus
    {
        get;
        init;
    }
}

internal sealed class SkuSeedItem
{
    public required string Sku { get; init; }

    public required string Flavor { get; init; }

    public required int VolumeMl { get; init; }

    public required string ContainerType { get; init; }

    public required int UnitsPerPack { get; init; }

    public required string PackLabel { get; init; }

    public required decimal PriceMxn { get; init; }
}