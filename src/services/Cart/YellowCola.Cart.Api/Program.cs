using YellowCola.Cart.Api.Endpoints;
using YellowCola.Cart.Api.Errors;
using YellowCola.Cart.Application.Carts;
using YellowCola.Cart.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CartExceptionHandler>();
builder.Services.AddScoped<CartApplicationService>();
builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseExceptionHandler();

app.MapGet("/health", () => Results.Ok(new { service = "cart", status = "healthy" }));
app.MapCartEndpoints();

app.Run();

public partial class Program;