using System.Text.Json.Serialization;

namespace ExoAuth.Application.Common.Models;

public sealed record ApiResponse<T>
{
    public string Status { get; init; } = "success";
    public int StatusCode { get; init; } = 200;
    public string Message { get; init; } = "OK";
    public T? Data { get; init; }
    public ApiResponseMeta? Meta { get; init; }
    public IReadOnlyList<ApiError>? Errors { get; init; }

    [JsonIgnore]
    public bool IsSuccess => Status == "success";

    public static ApiResponse<T> Success(T data, string message = "OK", int statusCode = 200)
    {
        return new ApiResponse<T>
        {
            Status = "success",
            StatusCode = statusCode,
            Message = message,
            Data = data,
            Meta = new ApiResponseMeta()
        };
    }

    public static ApiResponse<T> Success(T data, PaginationMeta pagination, string message = "OK")
    {
        return new ApiResponse<T>
        {
            Status = "success",
            StatusCode = 200,
            Message = message,
            Data = data,
            Meta = new ApiResponseMeta { Pagination = pagination }
        };
    }

    public static ApiResponse<T> Error(string message, int statusCode, IEnumerable<ApiError>? errors = null)
    {
        return new ApiResponse<T>
        {
            Status = "error",
            StatusCode = statusCode,
            Message = message,
            Errors = errors?.ToList(),
            Meta = new ApiResponseMeta()
        };
    }

    public static ApiResponse<T> Error(string message, int statusCode, ApiError error)
    {
        return Error(message, statusCode, new[] { error });
    }
}

public sealed record ApiResponseMeta
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string RequestId { get; init; } = string.Empty;
    public PaginationMeta? Pagination { get; init; }
}

public static class ApiResponse
{
    public static ApiResponse<object> Success(string message = "OK", int statusCode = 200)
    {
        return ApiResponse<object>.Success(null!, message, statusCode);
    }

    public static ApiResponse<object> Error(string message, int statusCode, IEnumerable<ApiError>? errors = null)
    {
        return ApiResponse<object>.Error(message, statusCode, errors);
    }

    public static ApiResponse<object> Error(string message, int statusCode, ApiError error)
    {
        return ApiResponse<object>.Error(message, statusCode, error);
    }
}
