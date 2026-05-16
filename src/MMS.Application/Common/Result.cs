namespace MMS.Application.Common;

public class Result<T>
{
    public bool Succeeded { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; } // Chuyển thành property thực thụ
    public List<string> Errors { get; set; } = new List<string>();

    public static Result<T> Success(T data, string? message = null) 
        => new Result<T> { Succeeded = true, Data = data, Message = message };

    public static Result<T> Failure(string error, int statusCode = 400) 
        => new Result<T> { Succeeded = false, Errors = new List<string> { error }, Message = error };

    public static Result<T> Failure(List<string> errors) 
        => new Result<T> { Succeeded = false, Errors = errors, Message = errors.FirstOrDefault() };
}
