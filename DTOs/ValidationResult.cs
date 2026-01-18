namespace SMHFR_BE.DTOs;

/// <summary>
/// Represents a validation result with errors
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Creates a valid result
    /// </summary>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates an invalid result with errors
    /// </summary>
    public static ValidationResult Failure(List<ValidationError> errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates an invalid result with a single error
    /// </summary>
    public static ValidationResult Failure(string field, string message)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError> { new ValidationError(field, message) }
        };
    }

    /// <summary>
    /// Adds an error to the validation result
    /// </summary>
    public void AddError(string field, string message)
    {
        IsValid = false;
        Errors.Add(new ValidationError(field, message));
    }

    /// <summary>
    /// Adds multiple errors to the validation result
    /// </summary>
    public void AddErrors(List<ValidationError> errors)
    {
        IsValid = false;
        Errors.AddRange(errors);
    }
}

/// <summary>
/// Represents a single validation error
/// </summary>
public class ValidationError
{
    public string Field { get; set; }
    public string Message { get; set; }

    public ValidationError(string field, string message)
    {
        Field = field;
        Message = message;
    }
}
