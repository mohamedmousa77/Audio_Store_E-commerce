namespace AudioStore.Domain.Exceptions;

/// <summary>
/// Exception thrown when access to a resource is forbidden
/// </summary>
public class ForbiddenException : DomainException
{
    public ForbiddenException(string message, string? errorCode = null)
        : base(message, errorCode ?? "FORBIDDEN")
    {
    }

    public ForbiddenException(string resourceName, string action)
        : base($"Access to {resourceName} for action '{action}' is forbidden", "FORBIDDEN")
    {
    }

    public ForbiddenException(string message, Exception innerException, string? errorCode = null)
        : base(message, innerException, errorCode ?? "FORBIDDEN")
    {
    }
}
