using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Application.Common.Exceptions;

namespace ProductCare.Api.Infrastructure.Errors;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails;

        switch (exception)
        {
            case ValidationException validationException:
                logger.LogWarning(exception, "Request validation failed. TraceId: {TraceId}", httpContext.TraceIdentifier);

                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "The request is invalid.",
                    Detail = validationException.Message,
                    Instance = httpContext.Request.Path
                };
                break;

            case ConflictException conflictException:
                logger.LogWarning(exception, "Request could not be completed because of an application conflict. TraceId: {TraceId}", httpContext.TraceIdentifier);

                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "The request conflicts with the current state.",
                    Detail = conflictException.Message,
                    Instance = httpContext.Request.Path
                };
                break;

            default:
                logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", httpContext.TraceIdentifier);

                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred.",
                    Detail = "The request could not be completed.",
                    Instance = httpContext.Request.Path
                };
                break;
        }

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}