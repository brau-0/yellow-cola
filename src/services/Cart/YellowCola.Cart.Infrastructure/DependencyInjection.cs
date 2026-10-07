using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

using YellowCola.Cart.Application.Carts;
using YellowCola.Cart.Infrastructure.Persistence;

namespace YellowCola.Cart.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Connection string 'Redis' was not found.");

            return ConnectionMultiplexer.Connect(connectionString);
        });

        services.AddSingleton<ICartRepository, RedisCartRepository>();

        return services;
    }
}