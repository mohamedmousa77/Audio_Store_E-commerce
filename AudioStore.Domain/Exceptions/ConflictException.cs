namespace AudioStore.Domain.Exceptions;

/// <summary>
/// Exception thrown when a conflict occurs (e.g., duplicate resource)
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message, string? errorCode = null)
        : base(message, errorCode ?? "CONFLICT")
    {
    }

    public ConflictException(string resourceName, string conflictReason)
        : base($"{resourceName} conflict: {conflictReason}", "CONFLICT")
    {
    }

    public ConflictException(string message, Exception innerException, string? errorCode = null)
        : base(message, innerException, errorCode ?? "CONFLICT")
    {
    }
}
