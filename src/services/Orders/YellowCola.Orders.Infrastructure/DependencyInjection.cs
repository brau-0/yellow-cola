using Azure.Messaging.ServiceBus;

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

        //services.AddSingleton<IIntegrationMessageTransport, LocalLoggingIntegrationMessageTransport>();

        services.AddSingleton<LocalLoggingIntegrationMessageTransport>();
        services.AddSingleton<AzureServiceBusIntegrationMessageTransport>();

        services.AddSingleton<ServiceBusClient>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration["Messaging:ServiceBus:ConnectionString"] ?? throw new InvalidOperationException("Service Bus connection string was not configured.");

            return new ServiceBusClient(connectionString);
        });

        services.AddSingleton<ServiceBusSender>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var queueName = configuration["Messaging:ServiceBus:InventoryCommandsQueue"] ?? throw new InvalidOperationException("Inventory commands queue was not configured.");

            return serviceProvider.GetRequiredService<ServiceBusClient>().CreateSender(queueName);
        });

        services.AddSingleton<IIntegrationMessageTransport>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var transport = configuration["Messaging:Transport"] ?? "Local";

            return transport switch
            {
                "Local" => serviceProvider.GetRequiredService<LocalLoggingIntegrationMessageTransport>(),
                "AzureServiceBus" => serviceProvider.GetRequiredService<AzureServiceBusIntegrationMessageTransport>(),
                _ => throw new InvalidOperationException($"Unsupported messaging transport '{transport}'.")
            };
        });

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