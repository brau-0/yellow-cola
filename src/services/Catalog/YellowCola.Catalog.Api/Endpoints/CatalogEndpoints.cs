using YellowCola.Catalog.Application.Queries;

namespace YellowCola.Catalog.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(
            "/api/catalog");

        group.MapGet(
            "/categories",
            GetCategoriesAsync);

        group.MapGet(
            "/products",
            SearchProductsAsync);

        group.MapGet(
            "/products/{slug}",
            GetProductBySlugAsync);

        return endpoints;
    }

    private static async Task<IResult>
        GetCategoriesAsync(
            ICatalogQueries queries,
            CancellationToken cancellationToken)
    {
        var categories =
            await queries.GetCategoriesAsync(
                cancellationToken);

        return Results.Ok(categories);
    }

    private static async Task<IResult>
        SearchProductsAsync(
            int? page,
            int? pageSize,
            string? brand,
            string? category,
            ICatalogQueries queries,
            CancellationToken cancellationToken)
    {
        var requestedPage = page ?? 1;
        var requestedPageSize = pageSize ?? 20;

        if (requestedPage < 1)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "Page must be greater than or equal to 1."
                });
        }

        if (requestedPageSize is < 1 or > 100)
        {
            return Results.BadRequest(
                new
                {
                    error =
                        "PageSize must be between 1 and 100."
                });
        }

        var request = new ProductSearchRequest(
            requestedPage,
            requestedPageSize,
            brand,
            category);

        var result =
            await queries.SearchProductsAsync(
                request,
                cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult>
        GetProductBySlugAsync(
            string slug,
            ICatalogQueries queries,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return Results.BadRequest();
        }

        var product =
            await queries.GetProductBySlugAsync(
                slug,
                cancellationToken);

        return product is null
            ? Results.NotFound()
            : Results.Ok(product);
    }
}