namespace MMS.Application.Common;

public class Result<T>
{
    public bool Succeeded { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public int StatusCode { get; set; } = 200;
    public Dictionary<string, string[]> Errors { get; set; } = new();

    public static Result<T> Success(T data, string? message = null)
        => new() { Succeeded = true, Data = data, Message = message, StatusCode = 200 };

    public static Result<T> Failure(string errorCode, string message, int statusCode = 400)
        => new() { Succeeded = false, ErrorCode = errorCode, Message = message, StatusCode = statusCode };

    public static Result<T> ValidationError(string message, Dictionary<string, string[]> errors)
        => new() { Succeeded = false, ErrorCode = "VALIDATION_ERROR", Message = message, StatusCode = 400, Errors = errors };

    public static Result<T> NotFound(string errorCode, string message)
        => new() { Succeeded = false, ErrorCode = errorCode, Message = message, StatusCode = 404 };

    public static Result<T> Forbidden(string errorCode, string message)
        => new() { Succeeded = false, ErrorCode = errorCode, Message = message, StatusCode = 403 };
}
