using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using YellowCola.Cart.Application.Carts;

namespace YellowCola.Cart.Api.Errors;

internal sealed class CartExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ArgumentException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = exception.Message
            },

            CartConflictException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Cart conflict",
                Detail = exception.Message
            },

            _ => null
        };

        if (problem is null) return false;

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}