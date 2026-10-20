using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace YellowCola.Orders.IntegrationTests.Infrastructure;

public sealed class OrdersWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");
        builder.UseSetting("ConnectionStrings:OrdersDatabase", connectionString);
        builder.UseSetting("OutboxPublisher:Enabled", "false");
    }
}