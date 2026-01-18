using AudioStore.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace AudioStore.Api.Middleware;

/// <summary>
/// Global exception handling middleware with production-ready features
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        // Log with correlation ID for tracking
        _logger.LogError(exception,
            "An unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}",
            correlationId, context.Request.Path);

        var (statusCode, errorCode, message, errors) = GetErrorDetails(exception);

        var response = new ErrorResponse
        {
            Success = false,
            Error = message,
            ErrorCode = errorCode,
            CorrelationId = correlationId,
            Errors = errors,
            // Only include details in Development
            Details = _environment.IsDevelopment() ? exception.Message : null,
            StackTrace = _environment.IsDevelopment() ? exception.StackTrace : null
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    private static (HttpStatusCode statusCode, string errorCode, string message, Dictionary<string, string[]>? errors) 
        GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            NotFoundException notFound => (
                HttpStatusCode.NotFound,
                notFound.ErrorCode ?? "NOT_FOUND",
                notFound.Message,
                null
            ),

            FluentValidation.ValidationException validation => (
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                validation.Message,
                validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    )
            ),

            DomainValidationException domainValidation => (
                HttpStatusCode.BadRequest,
                domainValidation.ErrorCode ?? "VALIDATION_ERROR",
                domainValidation.Message,
                domainValidation.ValidationErrors.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                )
            ),

            BadRequestException badRequest => (
                HttpStatusCode.BadRequest,
                badRequest.ErrorCode ?? "BAD_REQUEST",
                badRequest.Message,
                null
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "UNAUTHORIZED",
                "Unauthorized access. Please authenticate.",
                null
            ),

            ForbiddenException forbidden => (
                HttpStatusCode.Forbidden,
                forbidden.ErrorCode ?? "FORBIDDEN",
                forbidden.Message,
                null
            ),

            ConflictException conflict => (
                HttpStatusCode.Conflict,
                conflict.ErrorCode ?? "CONFLICT",
                conflict.Message,
                null
            ),

            DomainException domain => (
                HttpStatusCode.BadRequest,
                domain.ErrorCode ?? "DOMAIN_ERROR",
                domain.Message,
                null
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_SERVER_ERROR",
                "An internal server error occurred. Please try again later.",
                null
            )
        };
    }

    private class ErrorResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public Dictionary<string, string[]>? Errors { get; set; }
        public string? Details { get; set; }
        public string? StackTrace { get; set; }
    }
}
