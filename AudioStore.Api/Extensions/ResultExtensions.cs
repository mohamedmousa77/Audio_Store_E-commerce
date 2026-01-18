using AudioStore.Common.Result;
using Microsoft.AspNetCore.Mvc;

namespace AudioStore.Api.Extensions;

/// <summary>
/// Extension methods for converting Result objects to IActionResult
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a Result to an IActionResult with appropriate HTTP status code
    /// </summary>
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return result.StatusCode switch
            {
                204 => new NoContentResult(),
                _ => new ObjectResult(new { success = true })
                {
                    StatusCode = result.StatusCode
                }
            };
        }

        return new ObjectResult(new
        {
            success = false,
            error = result.Error,
            errorCode = result.ErrorCode,
            errors = result.Errors.Count > 0 ? result.Errors : null
        })
        {
            StatusCode = result.StatusCode
        };
    }

    /// <summary>
    /// Converts a Result<T> to an IActionResult with appropriate HTTP status code
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.StatusCode switch
            {
                204 => new NoContentResult(),
                _ => new ObjectResult(result.Value)
                {
                    StatusCode = result.StatusCode
                }
            };
        }

        return new ObjectResult(new
        {
            success = false,
            error = result.Error,
            errorCode = result.ErrorCode,
            errors = result.Errors.Count > 0 ? result.Errors : null
        })
        {
            StatusCode = result.StatusCode
        };
    }

    /// <summary>
    /// Converts a Result<T> to an OkObjectResult (200) if successful
    /// </summary>
    public static IActionResult ToOkResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return new ObjectResult(new
        {
            success = false,
            error = result.Error,
            errorCode = result.ErrorCode,
            errors = result.Errors.Count > 0 ? result.Errors : null
        })
        {
            StatusCode = result.StatusCode
        };
    }

    /// <summary>
    /// Converts a Result<T> to a CreatedAtActionResult (201) if successful
    /// </summary>
    public static IActionResult ToCreatedResult<T>(
        this Result<T> result,
        string actionName,
        object? routeValues = null)
    {
        if (result.IsSuccess)
            return new CreatedAtActionResult(actionName, null, routeValues, result.Value);

        return new ObjectResult(new
        {
            success = false,
            error = result.Error,
            errorCode = result.ErrorCode,
            errors = result.Errors.Count > 0 ? result.Errors : null
        })
        {
            StatusCode = result.StatusCode
        };
    }

    /// <summary>
    /// Matches the result and executes the appropriate function
    /// </summary>
    public static TOut Match<T, TOut>(
        this Result<T> result,
        Func<T, TOut> onSuccess,
        Func<string, TOut> onFailure)
    {
        return result.IsSuccess
            ? onSuccess(result.Value!)
            : onFailure(result.Error!);
    }

    /// <summary>
    /// Executes an action based on the result status
    /// </summary>
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value!);

        return result;
    }

    /// <summary>
    /// Executes an action if the result is a failure
    /// </summary>
    public static Result<T> OnFailure<T>(this Result<T> result, Action<string> action)
    {
        if (result.IsFailure)
            action(result.Error!);

        return result;
    }
}
