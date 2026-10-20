using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using YellowCola.Orders.Application.Checkout;
using YellowCola.Orders.Application.Messaging;
using YellowCola.Orders.Application.Orders;
using YellowCola.Orders.Application.Persistence;
using YellowCola.Orders.Infrastructure.Checkout;
using YellowCola.Orders.Infrastructure.Messaging;
using YellowCola.Orders.Infrastructure.Persistence;
using YellowCola.Orders.Infrastructure.Persistence.Outbox;
using YellowCola.Orders.Infrastructure.Repositories;

namespace YellowCola.Orders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<OrdersDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("OrdersDatabase") ?? throw new InvalidOperationException("Connection string 'OrdersDatabase' was not found.");

            options.UseNpgsql(connectionString);
        });

        services.AddHttpClient<ICheckoutCartReader, HttpCheckoutCartReader>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            client.BaseAddress = GetBaseAddress(configuration, "Cart");
            client.Timeout = TimeSpan.FromSeconds(3);
        });

        services.AddHttpClient<ICheckoutCatalogReader, HttpCheckoutCatalogReader>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            client.BaseAddress = GetBaseAddress(configuration, "Catalog");
            client.Timeout = TimeSpan.FromSeconds(3);
        });

        services.AddHttpClient<ICheckoutCustomerReader, HttpCheckoutCustomerReader>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            client.BaseAddress = GetBaseAddress(configuration, "Customer");
            client.Timeout = TimeSpan.FromSeconds(3);
        });

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrdersUnitOfWork, OrdersUnitOfWork>();
        services.AddScoped<ICheckoutSagaRepository, CheckoutSagaRepository>();
        services.AddScoped<IOutboxWriter, EfOutboxWriter>();

        services.AddSingleton<IIntegrationMessageTransport, LocalLoggingIntegrationMessageTransport>();
        services.AddScoped<OutboxPublisherProcessor>();
        services.AddHostedService<OutboxPublisherHostedService>();

        return services;
    }
    private static Uri GetBaseAddress(IConfiguration configuration, string serviceName)
    {
        var value = configuration[$"Services:{serviceName}:BaseUrl"]
            ?? throw new InvalidOperationException($"Service URL 'Services:{serviceName}:BaseUrl' was not found.");

        if (!value.EndsWith('/')) value += "/";

        return new Uri(value, UriKind.Absolute);
    }
}