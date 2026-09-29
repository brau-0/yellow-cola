using YellowCola.Catalog.Infrastructure;
using YellowCola.Catalog.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(
    builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var seeder =
        scope.ServiceProvider
            .GetRequiredService<CatalogSeeder>();

    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        service = "catalog",
        status = "healthy"
    });
});

app.Run();

public partial class Program;