namespace YellowCola.Catalog.Application.Queries
{
    public sealed record CategoryDto(
    string Name,
    string Slug,
    string? ParentSlug);
    public sealed record ProductSummaryDto(
    string Code,
    string Brand,
    string Name,
    string Slug,
    string ShortDescription,
    string PrimaryImageKey,
    decimal StartingPrice,
    string Currency,
    int SkuCount);

    public sealed record SkuDto(
        string Code,
        string Flavor,
        int VolumeMl,
        string ContainerType,
        int UnitsPerPack,
        string PackLabel,
        decimal Price,
        string Currency);

    public sealed record ProductDetailDto(
        string Code,
        string Brand,
        string Name,
        string Slug,
        string Category,
        string CategorySlug,
        string ShortDescription,
        string Description,
        string PrimaryImageKey,
        IReadOnlyCollection<SkuDto> Skus);

    public sealed record PagedResult<T>(
        IReadOnlyCollection<T> Items,
        int Page,
        int PageSize,
        int TotalCount)
    {
        public int TotalPages =>
            (int)Math.Ceiling(
                TotalCount / (double)PageSize);
    }
}
