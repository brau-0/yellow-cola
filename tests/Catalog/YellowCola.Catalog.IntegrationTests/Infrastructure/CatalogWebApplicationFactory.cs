using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace YellowCola.Catalog.IntegrationTests.Infrastructure;

public sealed class CatalogWebApplicationFactory(
    string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");

        builder.UseSetting(
            "ConnectionStrings:CatalogDatabase",
            connectionString);
    }
}
