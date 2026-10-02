using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YellowCola.Inventory.Application.Inventory;
using YellowCola.Inventory.Application.Persistence;
using YellowCola.Inventory.Infrastructure.Repositories;

using YellowCola.Inventory.Infrastructure.Persistence;

namespace YellowCola.Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddDbContext<InventoryDbContext>(
            (serviceProvider, options) =>
            {
                var configuration =
                    serviceProvider
                        .GetRequiredService<IConfiguration>();

                var connectionString =
                    configuration.GetConnectionString(
                        "InventoryDatabase")
                    ?? throw new InvalidOperationException(
                        "Connection string 'InventoryDatabase' was not found.");

                options.UseNpgsql(
                    connectionString);
            });
        
        services.AddScoped< IInventoryRepository, InventoryRepository>();

        services.AddScoped< IInventoryUnitOfWork,InventoryUnitOfWork>();

        return services;
    }
}