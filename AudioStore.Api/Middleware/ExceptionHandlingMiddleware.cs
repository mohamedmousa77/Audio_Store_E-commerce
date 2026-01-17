using AudioStore.Common.Constants;
using System.Net;
using System.Text.Json;

namespace AudioStore.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var errorCode = ErrorCode.InternalServerError;
        var message = "An internal server error occurred";

        if (exception is UnauthorizedAccessException)
        {
            code = HttpStatusCode.Unauthorized;
            errorCode = ErrorCode.Unauthorized;
            message = "Unauthorized access";
        }

        var result = JsonSerializer.Serialize(new
        {
            error = message,
            errorCode,
            details = exception.Message
        });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(result);
    }


}
