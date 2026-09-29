using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;

using YellowCola.Catalog.Application.Queries;
using YellowCola.Catalog.Infrastructure.Persistence;

namespace YellowCola.Catalog.Infrastructure.Queries;

internal sealed class CatalogQueries(
    CatalogDbContext dbContext)
    : ICatalogQueries
{
    public async Task<IReadOnlyCollection<CategoryDto>>
        GetCategoriesAsync(
            CancellationToken cancellationToken = default)
    {
        var query =
            from category in dbContext.Categories.AsNoTracking()
            join parent in dbContext.Categories.AsNoTracking()
                on category.ParentId equals parent.Id
                into parentGroup
            from parent in parentGroup.DefaultIfEmpty()
            where category.IsActive
            orderby category.ParentId, category.Name
            select new CategoryDto(
                category.Name,
                category.Slug,
                parent == null
                    ? null
                    : parent.Slug);

        return await query.ToListAsync(
            cancellationToken);
    }

    public async Task<PagedResult<ProductSummaryDto>>
        SearchProductsAsync(
            ProductSearchRequest request,
            CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Brand))
        {
            var brand = request.Brand.Trim();

            query = query.Where(
                product =>
                    EF.Functions.ILike(
                        product.Brand,
                        brand));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var categorySlug =
                request.Category
                    .Trim()
                    .ToLowerInvariant();

            var categoryIds =
                from category in dbContext.Categories.AsNoTracking()
                join parent in dbContext.Categories.AsNoTracking()
                    on category.ParentId equals parent.Id
                    into parentGroup
                from parent in parentGroup.DefaultIfEmpty()
                where
                    category.Slug == categorySlug ||
                    (parent != null &&
                     parent.Slug == categorySlug)
                select category.Id;

            query = query.Where(
                product =>
                    categoryIds.Contains(
                        product.CategoryId));
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var items = await query
            .OrderBy(product => product.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(product =>
                new ProductSummaryDto(
                    product.Code,
                    product.Brand,
                    product.Name,
                    product.Slug,
                    product.ShortDescription,
                    product.PrimaryImageKey,

                    product.Skus
                        .Where(sku => sku.IsActive)
                        .Select(sku => (decimal?)sku.Price)
                        .Min() ?? 0m,

                    "MXN",

                    product.Skus.Count(
                        sku => sku.IsActive)))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductSummaryDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task<ProductDetailDto?>
        GetProductBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
    {
        var normalizedSlug =
            slug.Trim().ToLowerInvariant();

        var query =
            from product in dbContext.Products.AsNoTracking()
            join category in dbContext.Categories.AsNoTracking()
                on product.CategoryId equals category.Id
            where
                product.IsActive &&
                product.Slug == normalizedSlug
            select new ProductDetailDto(
                product.Code,
                product.Brand,
                product.Name,
                product.Slug,
                category.Name,
                category.Slug,
                product.ShortDescription,
                product.Description,
                product.PrimaryImageKey,

                product.Skus
                    .Where(sku => sku.IsActive)
                    .OrderBy(sku => sku.UnitSizeMl)
                    .ThenBy(sku => sku.UnitsPerPack)
                    .Select(sku =>
                        new SkuDto(
                            sku.Code,
                            sku.Flavor,
                            sku.UnitSizeMl,
                            sku.PackageType,
                            sku.UnitsPerPack,
                            sku.PackLabel,
                            sku.Price,
                            sku.Currency))
                    .ToList());

        return await query.SingleOrDefaultAsync(
            cancellationToken);
    }
}