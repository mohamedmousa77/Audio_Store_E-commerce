namespace AudioStore.Domain.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message, string? errorCode = null)
        : base(message, errorCode)
    {
    }

    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with key '{key}' was not found", "NOT_FOUND")
    {
    }

    public NotFoundException(string message, Exception innerException, string? errorCode = null)
        : base(message, innerException, errorCode)
    {
    }
}
