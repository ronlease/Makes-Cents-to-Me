using MakesCentsToMe.Api.Common;

namespace MakesCentsToMe.Api.Features.LearnedRules;

/// <summary>
/// Wraps an <see cref="ApiResponse{T}"/> and records whether a failure was caused by a missing
/// resource, so endpoints can distinguish 404 from 400.
/// </summary>
public class LearnedRuleResult<T>
{
    public bool IsNotFound { get; init; }
    public ApiResponse<T> Response { get; init; } = null!;
    public bool Success => Response.Success;

    public static LearnedRuleResult<T> NotFound(string error) =>
        new() { IsNotFound = true, Response = ApiResponse<T>.Fail(error) };

    public static LearnedRuleResult<T> Ok(T data) =>
        new() { Response = ApiResponse<T>.Ok(data) };

    public static LearnedRuleResult<T> ValidationFailure(string error) =>
        new() { Response = ApiResponse<T>.Fail(error) };
}
