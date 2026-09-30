using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace YellowCola.Customer
    .IntegrationTests.Infrastructure;

public sealed class CustomerWebApplicationFactory(
    string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment(
            "IntegrationTesting");

        builder.UseSetting(
            "ConnectionStrings:CustomerDatabase",
            connectionString);
    }
}