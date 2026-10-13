using Microsoft.AspNetCore.Diagnostics;

using YellowCola.Orders.Application.Checkout;

namespace YellowCola.Orders.Api.Errors;

internal sealed partial class OrdersExceptionHandler(ILogger<OrdersExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            CheckoutException checkoutException when checkoutException.FailureType == CheckoutFailureType.NotFound => StatusCodes.Status404NotFound,
            CheckoutException => StatusCodes.Status409Conflict,
            ArgumentException => StatusCodes.Status400BadRequest,
            HttpRequestException => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode >= 500) LogRequestFailed(logger, exception, statusCode);
        else LogRequestRejected(logger, exception, statusCode);

        var title = statusCode switch
        {
            StatusCodes.Status400BadRequest => "Invalid request",
            StatusCodes.Status404NotFound => "Checkout resource not found",
            StatusCodes.Status409Conflict => "Checkout conflict",
            StatusCodes.Status503ServiceUnavailable => "Dependency unavailable",
            _ => "Unexpected error"
        };

        await Results.Problem(statusCode: statusCode, title: title, detail: exception.Message).ExecuteAsync(httpContext);

        return true;
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "Orders request failed with status code {StatusCode}.")]
    private static partial void LogRequestFailed(ILogger logger, Exception exception, int statusCode);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Orders request was rejected with status code {StatusCode}.")]
    private static partial void LogRequestRejected(ILogger logger, Exception exception, int statusCode);
}