using YellowCola.Cart.Application.Carts;
using YellowCola.Cart.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<CartApplicationService>();
builder.Services.AddInfrastructure();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { service = "cart", status = "healthy" }));

app.Run();

public partial class Program;