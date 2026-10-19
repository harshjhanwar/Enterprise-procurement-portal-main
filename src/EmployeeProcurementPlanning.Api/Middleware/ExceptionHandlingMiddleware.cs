using System.Net;
using System.Text.Json;

namespace EmployeeProcurementPlanning.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var incomingCorrelationId) &&
            !string.IsNullOrWhiteSpace(incomingCorrelationId))
        {
            context.TraceIdentifier = incomingCorrelationId.ToString();
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            return Task.CompletedTask;
        });

        try
        {
            await next(context);
        }
        catch (ArgumentException exception)
        {
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "validation_error", exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            await WriteProblemAsync(context, HttpStatusCode.NotFound, "not_found", exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            await WriteProblemAsync(context, HttpStatusCode.Forbidden, "forbidden", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            await WriteProblemAsync(context, HttpStatusCode.Conflict, "conflict", exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled exception. TraceId: {TraceId}",
                context.TraceIdentifier);

            await WriteProblemAsync(
                context,
                HttpStatusCode.InternalServerError,
                "server_error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string type,
        string detail)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var response = new
        {
            type,
            title = type,
            detail,
            status = (int)statusCode,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
