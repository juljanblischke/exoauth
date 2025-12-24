namespace ExoAuth.Application.Common.Models;

public static class ErrorCodes
{
    // Validation Errors (400)
    public const string ValidationRequired = "VALIDATION_REQUIRED";
    public const string ValidationInvalidFormat = "VALIDATION_INVALID_FORMAT";
    public const string ValidationMinLength = "VALIDATION_MIN_LENGTH";
    public const string ValidationMaxLength = "VALIDATION_MAX_LENGTH";

    // Authentication Errors (401)
    public const string AuthUnauthorized = "AUTH_UNAUTHORIZED";
    public const string AuthTokenExpired = "AUTH_TOKEN_EXPIRED";
    public const string AuthTokenInvalid = "AUTH_TOKEN_INVALID";

    // Authorization Errors (403)
    public const string AuthForbidden = "AUTH_FORBIDDEN";

    // Resource Errors (404)
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";

    // Rate Limiting (429)
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";

    // Server Errors (500)
    public const string InternalError = "INTERNAL_ERROR";
}
