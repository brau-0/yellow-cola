using YellowCola.Inventory.Application.Inventory;
using YellowCola.Inventory.Application.Messaging;
using YellowCola.Inventory.Infrastructure;



var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(
    TimeProvider.System);

builder.Services.AddScoped<
    InventoryApplicationService>();

builder.Services.AddInfrastructure();

builder.Services.AddScoped<ReserveInventoryMessageHandler>();

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