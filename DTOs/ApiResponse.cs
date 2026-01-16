namespace SMHFR_BE.DTOs;

/// <summary>
/// Generic API response wrapper
/// </summary>
/// <typeparam name="T">Type of the data payload</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static ApiResponse<T> SuccessResult(T data, string message = "Operation completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a successful response without data
    /// </summary>
    public static ApiResponse<T> SuccessResult(string message = "Operation completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static ApiResponse<T> ErrorResult(string error, List<string>? additionalErrors = null)
    {
        var errors = new List<string> { error };
        if (additionalErrors != null && additionalErrors.Any())
        {
            errors.AddRange(additionalErrors);
        }

        return new ApiResponse<T>
        {
            Success = false,
            Message = "An error occurred",
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an error response with multiple errors
    /// </summary>
    public static ApiResponse<T> ErrorResult(List<string> errors, string message = "An error occurred")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Non-generic API response for operations without data
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// Creates a successful response without data
    /// </summary>
    public static new ApiResponse SuccessResult(string message = "Operation completed successfully")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static new ApiResponse ErrorResult(string error, List<string>? additionalErrors = null)
    {
        var errors = new List<string> { error };
        if (additionalErrors != null && additionalErrors.Any())
        {
            errors.AddRange(additionalErrors);
        }

        return new ApiResponse
        {
            Success = false,
            Message = "An error occurred",
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };
    }
}
