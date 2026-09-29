namespace YellowCola.Catalog.Application.Queries;

public interface ICatalogQueries
{
    Task<IReadOnlyCollection<CategoryDto>>
        GetCategoriesAsync(
            CancellationToken cancellationToken = default);

    Task<PagedResult<ProductSummaryDto>>
        SearchProductsAsync(
            ProductSearchRequest request,
            CancellationToken cancellationToken = default);

    Task<ProductDetailDto?>
        GetProductBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default);
}