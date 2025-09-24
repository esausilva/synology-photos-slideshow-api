using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Synology.Photos.Slideshow.Api.Middleware;

public sealed class GlobalExceptionHandlerMiddleware(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandlerMiddleware> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occured");

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#status.500",
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occured",
                Detail = exception.Message
            }
        });
    }
}