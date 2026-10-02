using YellowCola.Inventory.Infrastructure;
using YellowCola.Inventory.Application.Inventory;



var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(
    TimeProvider.System);

builder.Services.AddScoped<
    InventoryApplicationService>();

builder.Services.AddInfrastructure();

var app =
    builder.Build();

app.MapGet(
    "/health",
    () =>
    {
        return Results.Ok(
            new
            {
                service = "inventory",
                status = "healthy"
            });
    });

app.Run();

public partial class Program;