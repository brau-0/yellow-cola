using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace YellowCola.Inventory
    .IntegrationTests.Infrastructure;

public sealed class InventoryWebApplicationFactory(
    string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment(
            "IntegrationTesting");

        builder.UseSetting(
            "ConnectionStrings:InventoryDatabase",
            connectionString);
    }
}