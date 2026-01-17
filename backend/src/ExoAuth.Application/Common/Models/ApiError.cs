namespace ExoAuth.Application.Common.Models;

public sealed record ApiError
{
    public string? Field { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public static ApiError Create(string code, string message, string? field = null)
    {
        return new ApiError
        {
            Field = field,
            Code = code,
            Message = message
        };
    }

    public static ApiError Validation(string field, string code, string message)
    {
        return new ApiError
        {
            Field = field,
            Code = code,
            Message = message
        };
    }
}
