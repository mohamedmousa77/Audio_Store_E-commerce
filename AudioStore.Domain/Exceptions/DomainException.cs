namespace AudioStore.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-specific exceptions
/// </summary>
public abstract class DomainException : Exception
{
    public string? ErrorCode { get; }

    protected DomainException(string message, string? errorCode = null)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    protected DomainException(string message, Exception innerException, string? errorCode = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
