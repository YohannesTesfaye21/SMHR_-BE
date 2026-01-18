using Npgsql;
using SMHFR_BE.DTOs;
using System.Text.Json;

namespace SMHFR_BE.Middleware;

public class DatabaseErrorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DatabaseErrorMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public DatabaseErrorMiddleware(RequestDelegate next, ILogger<DatabaseErrorMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Extract PostgreSQL exception from exception chain (handles DbUpdateException wrapping)
            Npgsql.PostgresException? pgEx = ExtractPostgresException(ex);
            NpgsqlException? npgsqlEx = ExtractNpgsqlException(ex);
            
            // Only handle connection/auth errors - let data validation errors pass through
            if (pgEx != null || npgsqlEx != null)
            {
                string? sqlState = pgEx?.SqlState;
                bool isConnectionError = false;

                if (pgEx != null)
                {
                    // Check if this is a connection/authentication error (not a data validation error)
                    isConnectionError = IsConnectionErrorState(sqlState!);
                }
                else if (npgsqlEx != null)
                {
                    // NpgsqlException is typically a connection error
                    isConnectionError = true;
                }

                // Only handle connection/auth errors - data validation errors should bubble up to controllers
                if (isConnectionError)
                {
                    var exception = (Exception?)pgEx ?? npgsqlEx!;
                    _logger.LogError(exception, "❌ Database connection error detected (SQL State: {SqlState}) at {Path}", sqlState ?? "N/A", context.Request.Path);
                    
                    // Clear connection pool ONLY for connection/auth errors
                    try
                    {
                        NpgsqlConnection.ClearAllPools();
                        _logger.LogInformation("✅ Connection pool cleared due to connection error (SQL State: {SqlState})", sqlState ?? "N/A");
                    }
                    catch (Exception clearEx)
                    {
                        _logger.LogError(clearEx, "Failed to clear connection pool");
                    }
                    
                    // Handle password authentication errors specifically
                    if (pgEx != null && pgEx.SqlState == "28P01")
                    {
                        if (!context.Response.HasStarted)
                        {
                            await HandleDatabaseAuthenticationErrorAsync(context);
                            return; // Exit early - response already sent
                        }
                    }
                    // Re-throw connection errors (controllers handle them)
                    throw;
                }
                // For data validation errors (23505, etc.), let them pass through - don't re-throw here
            }
            
            // Re-throw all other exceptions (including data validation errors) - controllers handle them
            throw;
        }
    }

    /// <summary>
    /// Extracts PostgresException from exception chain (handles DbUpdateException wrapping)
    /// </summary>
    private static Npgsql.PostgresException? ExtractPostgresException(Exception ex)
    {
        if (ex is Npgsql.PostgresException directPgEx)
        {
            return directPgEx;
        }
        
        // Check nested exceptions recursively
        var inner = ex.InnerException;
        while (inner != null)
        {
            if (inner is Npgsql.PostgresException pgEx)
            {
                return pgEx;
            }
            inner = inner.InnerException;
        }
        
        return null;
    }

    /// <summary>
    /// Extracts NpgsqlException from exception chain (handles wrapping)
    /// </summary>
    private static NpgsqlException? ExtractNpgsqlException(Exception ex)
    {
        if (ex is NpgsqlException directNpgsqlEx)
        {
            return directNpgsqlEx;
        }
        
        // Check nested exceptions recursively
        var inner = ex.InnerException;
        while (inner != null)
        {
            if (inner is NpgsqlException npgsqlEx)
            {
                return npgsqlEx;
            }
            inner = inner.InnerException;
        }
        
        return null;
    }

    /// <summary>
    /// Determines if a SQL state code represents a connection/authentication error
    /// (as opposed to a data validation error)
    /// </summary>
    private static bool IsConnectionErrorState(string sqlState)
    {
        // Connection and authentication errors
        return sqlState switch
        {
            // Authentication errors
            "28P01" => true, // password authentication failed
            "28000" => true, // invalid authorization specification
            
            // Connection errors
            "08003" => true, // connection does not exist
            "08006" => true, // connection failure
            "08001" => true, // sqlclient_unable_to_establish_sqlconnection
            "57P01" => true, // admin shutdown
            "57P02" => true, // crash shutdown
            "57P03" => true, // cannot connect now
            "53300" => true, // too_many_connections
            
            // Data validation errors - DO NOT clear pool for these
            "23505" => false, // unique_violation (duplicate key)
            "23503" => false, // foreign_key_violation
            "23502" => false, // not_null_violation
            "23514" => false, // check_violation
            "23P01" => false, // exclusion_violation
            
            // Default: assume connection error for unknown states starting with connection error prefixes
            _ => sqlState.StartsWith("08") || sqlState.StartsWith("28") || sqlState.StartsWith("57P")
        };
    }

    private static async Task HandleDatabaseAuthenticationErrorAsync(HttpContext context)
    {
        var errorResponse = ApiResponse<object>.ErrorResult(
            "Database authentication failed. Please check database credentials.",
            new List<string>
            {
                "28P01: password authentication failed for user \"postgres\"",
                "This error occurs when the PostgreSQL password in docker-compose.yml doesn't match the password stored in the database volume.",
                "To fix: Reset the PostgreSQL volume or update the database password to match the connection string."
            }
        );

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(errorResponse, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}
