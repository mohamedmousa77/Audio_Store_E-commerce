namespace AudioStore.Domain.Exceptions;

/// <summary>
/// Exception thrown when a bad request is made
/// </summary>
public class BadRequestException : DomainException
{
    public BadRequestException(string message, string? errorCode = null)
        : base(message, errorCode ?? "BAD_REQUEST")
    {
    }

    public BadRequestException(string message, Exception innerException, string? errorCode = null)
        : base(message, innerException, errorCode ?? "BAD_REQUEST")
    {
    }
}
