using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Application.Common.Exceptions;
using System.Diagnostics;
using System.Globalization;

namespace ProductCare.Api.Infrastructure.Errors;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, TimeProvider timeProvider) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int statusCode = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            ConflictException => StatusCodes.Status409Conflict,
            QuotaExceededException => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogWarning(exception, "Request could not be completed. StatusCode: {StatusCode}. TraceId: {TraceId}", statusCode, httpContext.TraceIdentifier);
        }

        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = statusCode == StatusCodes.Status500InternalServerError ? "The request could not be completed." : exception.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (exception is QuotaExceededException quotaException)
        {
            DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
            int retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((quotaException.ResetUtc - utcNow).TotalSeconds));

            httpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

            problemDetails.Detail = "The daily usage limit for this ProductCare AI feature has been reached. Try again after the quota resets.";
            problemDetails.Extensions["operation"] = quotaException.Operation.ToString();
            problemDetails.Extensions["dailyLimit"] = quotaException.DailyLimit;
            problemDetails.Extensions["retryAfterUtc"] = quotaException.ResetUtc;
        }

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "The request is invalid.",
        StatusCodes.Status409Conflict => "The request conflicts with the current state.",
        StatusCodes.Status429TooManyRequests => "Daily AI usage limit reached.",
        _ => "An unexpected error occurred."
    };
}