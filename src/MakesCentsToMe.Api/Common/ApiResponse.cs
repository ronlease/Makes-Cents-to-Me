namespace MakesCentsToMe.Api.Common;

public class ApiResponse<T>
{
    public T? Data { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public bool Success { get; init; }

    public static ApiResponse<T> Fail(string error) =>
        new() { Errors = [error], Success = false };

    public static ApiResponse<T> Fail(IReadOnlyList<string> errors) =>
        new() { Errors = errors, Success = false };

    public static ApiResponse<T> Ok(T data) =>
        new() { Data = data, Success = true };
}
