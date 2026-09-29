using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using YellowCola.Catalog.Application.Queries;
using YellowCola.Catalog.Infrastructure.Persistence;
using YellowCola.Catalog.Infrastructure.Queries;
using YellowCola.Catalog.Infrastructure.Seed;

namespace YellowCola.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("CatalogDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'CatalogDatabase' was not found.");

        services.AddDbContext<CatalogDbContext>(
            options =>
            {
                options.UseNpgsql(connectionString);
            });

        services.AddScoped<ICatalogQueries, CatalogQueries>();
        services.AddScoped<CatalogSeeder>();
        return services;
    }
}
