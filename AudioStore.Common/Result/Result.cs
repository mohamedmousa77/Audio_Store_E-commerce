namespace AudioStore.Common;

/// <summary>
/// Represents the result of an operation without a return value
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public string? ErrorCode { get; }
    public int StatusCode { get; }
    public List<string> Errors { get; }

    protected Result(bool isSuccess, string? error, int statusCode, string? errorCode = null, List<string>? errors = null)
    {
        if (isSuccess && error != null)
            throw new InvalidOperationException("Success result cannot have an error");
        if (!isSuccess && error == null && (errors == null || errors.Count == 0))
            throw new InvalidOperationException("Failure result must have an error");

        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Errors = errors ?? new List<string>();
    }

    // Success factory methods
    public static Result Success(int statusCode = 200) 
        => new(true, null, statusCode);

    public static Result Created(int statusCode = 201) 
        => new(true, null, statusCode);

    public static Result NoContent() 
        => new(true, null, 204);

    // Failure factory methods
    public static Result Failure(string error, string? errorCode = null, int statusCode = 400)
        => new(false, error, statusCode, errorCode);

    public static Result BadRequest(string error, string? errorCode = null)
        => new(false, error, 400, errorCode);

    public static Result Unauthorized(string error = "Unauthorized access", string? errorCode = null)
        => new(false, error, 401, errorCode);

    public static Result Forbidden(string error = "Access forbidden", string? errorCode = null)
        => new(false, error, 403, errorCode);

    public static Result NotFound(string error = "Resource not found", string? errorCode = null)
        => new(false, error, 404, errorCode);

    public static Result Conflict(string error, string? errorCode = null)
        => new(false, error, 409, errorCode);

    public static Result ValidationError(List<string> errors, string? errorCode = null)
        => new(false, "Validation failed", 400, errorCode, errors);

    public static Result InternalError(string error = "An internal error occurred", string? errorCode = null)
        => new(false, error, 500, errorCode);

    // Generic result factory methods
    public static Result<T> Success<T>(T value, int statusCode = 200) 
        => new(value, true, null, statusCode);

    public static Result<T> Created<T>(T value, int statusCode = 201) 
        => new(value, true, null, statusCode);

    public static Result<T> Failure<T>(string error, string? errorCode = null, int statusCode = 400)
        => new(default!, false, error, statusCode, errorCode);

    public static Result<T> NotFound<T>(string error = "Resource not found", string? errorCode = null)
        => new(default!, false, error, 404, errorCode);

    public static Result<T> Unauthorized<T>(string error = "Unauthorized access", string? errorCode = null)
        => new(default!, false, error, 401, errorCode);

    public static Result<T> Forbidden<T>(string error = "Access forbidden", string? errorCode = null)
        => new(default!, false, error, 403, errorCode);

    public static Result<T> BadRequest<T>(string error, string? errorCode = null)
        => new(default!, false, error, 400, errorCode);

    public static Result<T> ValidationError<T>(List<string> errors, string? errorCode = null)
        => new(default!, false, "Validation failed", 400, errorCode, errors);
}

/// <summary>
/// Represents the result of an operation with a return value
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    protected internal Result(T? value, bool isSuccess, string? error, int statusCode, string? errorCode = null, List<string>? errors = null)
        : base(isSuccess, error, statusCode, errorCode, errors)
    {
        Value = value;
    }

    public static implicit operator Result<T>(T value) => Success(value);
}
