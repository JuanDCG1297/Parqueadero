using System.Text.Json;
using Domain.Exceptions;
using FluentValidation;

namespace Web.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        => (_next, _logger) = (next, logger);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemDetailsAsync(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteProblemDetailsAsync(context, StatusCodes.Status409Conflict, "Conflict", ex.Message);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            await WriteProblemDetailsAsync(context, StatusCodes.Status422UnprocessableEntity, "Validation Failed", "One or more validation errors occurred.", errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await WriteProblemDetailsAsync(context, StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, int status, string title, string detail, Dictionary<string, string[]>? errors = null)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;

        var problem = new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status,
            detail,
            errors,
            instance = context.Request.Path.ToString(),
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
