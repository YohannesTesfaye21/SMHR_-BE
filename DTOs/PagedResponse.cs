namespace SMHFR_BE.DTOs;

/// <summary>
/// Generic paginated response wrapper
/// </summary>
/// <typeparam name="T">Type of the data items in the list</typeparam>
public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResponse(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}

/// <summary>
/// Paginated API response wrapper
/// </summary>
/// <typeparam name="T">Type of the data items in the list</typeparam>
public class ApiPagedResponse<T> : ApiResponse<PagedResponse<T>>
{
    public ApiPagedResponse(PagedResponse<T>? data, string message = "Operation completed successfully") 
        : base()
    {
        Success = true;
        Message = message;
        Data = data;
        Timestamp = DateTime.UtcNow;
    }

    public static new ApiPagedResponse<T> SuccessResult(PagedResponse<T> data, string message = "Operation completed successfully")
    {
        return new ApiPagedResponse<T>(data, message);
    }

    public static new ApiPagedResponse<T> ErrorResult(string error, List<string>? additionalErrors = null)
    {
        var errors = new List<string> { error };
        if (additionalErrors != null && additionalErrors.Any())
        {
            errors.AddRange(additionalErrors);
        }

        return new ApiPagedResponse<T>(null, "An error occurred")
        {
            Success = false,
            Errors = errors
        };
    }
}