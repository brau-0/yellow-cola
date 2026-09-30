using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YellowCola.Customer.Application.Customers;
using YellowCola.Customer.Application.Queries;
using YellowCola.Customer.Infrastructure.Queries;
using YellowCola.Customer.Infrastructure.Repositories;

using YellowCola.Customer.Infrastructure.Persistence;

namespace YellowCola.Customer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddDbContext<CustomerDbContext>(
            (serviceProvider, options) =>
            {
                var configuration =
                    serviceProvider
                        .GetRequiredService<IConfiguration>();

                var connectionString =
                    configuration.GetConnectionString(
                        "CustomerDatabase")
                    ?? throw new InvalidOperationException(
                        "Connection string 'CustomerDatabase' was not found.");

                options.UseNpgsql(connectionString);
            });
        services.AddScoped<ICustomerRepository,CustomerRepository>();
        
        services.AddScoped<ICustomerQueries,CustomerQueries>();
        return services;
    }
}