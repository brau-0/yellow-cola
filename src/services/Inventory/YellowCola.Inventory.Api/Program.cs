using YellowCola.Inventory.Infrastructure;

var builder =
    WebApplication.CreateBuilder(args);

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