using Npgsql;

namespace SMHFR_BE.Middleware;

public class DatabaseErrorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DatabaseErrorMiddleware> _logger;

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
            // Check for PostgreSQL password authentication errors
            if (ex is Npgsql.PostgresException pgEx && pgEx.SqlState == "28P01")
            {
                _logger.LogError(pgEx, "Database password authentication error detected. Clearing connection pool...");
                
                try
                {
                    // Clear all connection pools to force new connections
                    NpgsqlConnection.ClearAllPools();
                    _logger.LogInformation("Connection pool cleared due to authentication error");
                }
                catch (Exception clearEx)
                {
                    _logger.LogError(clearEx, "Failed to clear connection pool");
                }
                
                // Re-throw to be handled by error handlers
                throw;
            }
            
            // Check for connection errors that might need pool clearing
            if (ex.InnerException is Npgsql.PostgresException innerPgEx && innerPgEx.SqlState == "28P01")
            {
                _logger.LogError(innerPgEx, "Database password authentication error in inner exception. Clearing connection pool...");
                
                try
                {
                    NpgsqlConnection.ClearAllPools();
                    _logger.LogInformation("Connection pool cleared due to authentication error in inner exception");
                }
                catch (Exception clearEx)
                {
                    _logger.LogError(clearEx, "Failed to clear connection pool");
                }
                
                throw;
            }
            
            // Re-throw other exceptions
            throw;
        }
    }
}
