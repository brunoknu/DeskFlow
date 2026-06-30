using DeskFlow.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DeskFlow.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Domain rule violation: {Message}", ex.Message);
            await WriteProblemAsync(context, StatusCodes.Status422UnprocessableEntity,
                "Business Rule Violation", ex.Message);
        }
        catch (Exception ex)
        {
            // Nunca expor detalhes internos ao cliente.
            var correlationId = context.TraceIdentifier;
            _logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please contact support.",
                correlationId);
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context, int statusCode, string title, string detail, string? correlationId = null)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (correlationId is not null)
            problem.Extensions["correlationId"] = correlationId;

        await context.Response.WriteAsJsonAsync(problem);
    }
}
