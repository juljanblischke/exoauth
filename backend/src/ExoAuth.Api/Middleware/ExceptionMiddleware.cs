using System.Security.Claims;
using System.Text.Json;
using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using FluentValidation;
using Serilog;

namespace ExoAuth.Api.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, auditService);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, IAuditService auditService)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => HandleValidationException(validationEx),
            AuthException authEx => HandleAuthException(authEx),
            Application.Common.Exceptions.SystemException systemEx => HandleSystemException(systemEx),
            UnauthorizedAccessException => HandleUnauthorizedException(),
            KeyNotFoundException => HandleNotFoundException(),
            _ => HandleUnknownException(exception)
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Request failed with status {StatusCode}: {Message}", statusCode, exception.Message);
        }

        // Audit log for 403 and 500 errors
        await LogErrorAuditAsync(context, statusCode, exception, auditService);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private static async Task LogErrorAuditAsync(HttpContext context, int statusCode, Exception exception, IAuditService auditService)
    {
        try
        {
            // Get user ID from claims if authenticated
            Guid? userId = null;
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            var endpoint = $"{context.Request.Method} {context.Request.Path}";
            var requestId = context.TraceIdentifier;

            if (statusCode == StatusCodes.Status403Forbidden)
            {
                await auditService.LogWithContextAsync(
                    AuditActions.AccessForbidden,
                    userId,
                    null,
                    null,
                    null,
                    new
                    {
                        Endpoint = endpoint,
                        RequestId = requestId,
                        ErrorCode = (exception as Application.Common.Exceptions.SystemException)?.ErrorCode ?? "FORBIDDEN"
                    },
                    context.RequestAborted
                );
            }
            else if (statusCode >= 500)
            {
                await auditService.LogWithContextAsync(
                    AuditActions.ErrorInternal,
                    userId,
                    null,
                    null,
                    null,
                    new
                    {
                        Endpoint = endpoint,
                        RequestId = requestId,
                        ErrorType = exception.GetType().Name
                    },
                    context.RequestAborted
                );
            }
        }
        catch (Exception ex)
        {
            // Don't let audit logging failures break the response
            // Just log and continue
            Log.Warning(ex, "Failed to write error audit log");
        }
    }

    private static (int, ApiResponse<object>) HandleValidationException(ValidationException exception)
    {
        var errors = exception.Errors
            .Select(e => ApiError.Validation(
                e.PropertyName,
                MapValidationErrorCode(e.ErrorCode),
                e.ErrorMessage))
            .ToList();

        return (StatusCodes.Status400BadRequest,
            ApiResponse<object>.Error("Validation failed", StatusCodes.Status400BadRequest, errors));
    }

    private static (int, ApiResponse<object>) HandleAuthException(AuthException exception)
    {
        // Special handling for AccountLockedException to include lockedUntil
        if (exception is AccountLockedException lockedException)
        {
            return (exception.StatusCode,
                new ApiResponse<object>
                {
                    Status = "error",
                    StatusCode = exception.StatusCode,
                    Message = exception.Message,
                    Data = lockedException.LockedUntil.HasValue
                        ? new { lockedUntil = lockedException.LockedUntil.Value }
                        : null,
                    Errors = new[] { ApiError.Create(exception.ErrorCode, exception.Message) },
                    Meta = new ApiResponseMeta()
                });
        }

        return (exception.StatusCode,
            ApiResponse<object>.Error(
                exception.Message,
                exception.StatusCode,
                ApiError.Create(exception.ErrorCode, exception.Message)));
    }

    private static (int, ApiResponse<object>) HandleSystemException(Application.Common.Exceptions.SystemException exception)
    {
        return (exception.StatusCode,
            ApiResponse<object>.Error(
                exception.Message,
                exception.StatusCode,
                ApiError.Create(exception.ErrorCode, exception.Message)));
    }

    private static (int, ApiResponse<object>) HandleUnauthorizedException()
    {
        return (StatusCodes.Status401Unauthorized,
            ApiResponse<object>.Error(
                "Unauthorized",
                StatusCodes.Status401Unauthorized,
                ApiError.Create(ErrorCodes.AuthUnauthorized, "Authentication is required to access this resource")));
    }

    private static (int, ApiResponse<object>) HandleNotFoundException()
    {
        return (StatusCodes.Status404NotFound,
            ApiResponse<object>.Error(
                "Resource not found",
                StatusCodes.Status404NotFound,
                ApiError.Create(ErrorCodes.ResourceNotFound, "The requested resource was not found")));
    }

    private (int, ApiResponse<object>) HandleUnknownException(Exception exception)
    {
        return (StatusCodes.Status500InternalServerError,
            ApiResponse<object>.Error(
                "An internal server error occurred",
                StatusCodes.Status500InternalServerError,
                ApiError.Create(ErrorCodes.InternalError, "An unexpected error occurred. Please try again later.")));
    }

    private static string MapValidationErrorCode(string fluentValidationCode)
    {
        return fluentValidationCode switch
        {
            "NotEmptyValidator" => ErrorCodes.ValidationRequired,
            "NotNullValidator" => ErrorCodes.ValidationRequired,
            "EmailValidator" => ErrorCodes.ValidationInvalidFormat,
            "MinimumLengthValidator" => ErrorCodes.ValidationMinLength,
            "MaximumLengthValidator" => ErrorCodes.ValidationMaxLength,
            _ => ErrorCodes.ValidationInvalidFormat
        };
    }
}
