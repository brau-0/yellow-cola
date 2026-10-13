using YellowCola.Orders.Api.Endpoints;
using YellowCola.Orders.Api.Errors;
using YellowCola.Orders.Application.Checkout;
using YellowCola.Orders.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<OrdersExceptionHandler>();

builder.Services.AddScoped<CheckoutApplicationService>();
builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseExceptionHandler();

app.MapGet("/health", () => Results.Ok(new { service = "orders", status = "healthy" }));
app.MapOrderEndpoints();

app.Run();

public partial class Program;