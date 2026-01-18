namespace AudioStore.Domain.Exceptions;

/// <summary>
/// Exception thrown when domain validation fails
/// </summary>
public class DomainValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

    public DomainValidationException(string message, string? errorCode = null)
        : base(message, errorCode ?? "VALIDATION_ERROR")
    {
        ValidationErrors = new Dictionary<string, string[]>();
    }

    public DomainValidationException(IDictionary<string, string[]> errors, string? errorCode = null)
        : base("One or more validation errors occurred", errorCode ?? "VALIDATION_ERROR")
    {
        ValidationErrors = new Dictionary<string, string[]>(errors);
    }

    public DomainValidationException(string propertyName, string errorMessage, string? errorCode = null)
        : base($"Validation failed for '{propertyName}': {errorMessage}", errorCode ?? "VALIDATION_ERROR")
    {
        ValidationErrors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
    }
}
