using Microsoft.EntityFrameworkCore;
using SMHFR_BE.Data;
using Npgsql;

namespace SMHFR_BE.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(ApplicationDbContext context, ILogger<HealthCheckService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public object CheckHealth()
    {
        var healthStatus = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "SMHFR Backend API",
            database = "unknown"
        };

        try
        {
            // Test database connection
            var canConnect = _context.Database.CanConnect();
            
            if (canConnect)
            {
                // Try a simple query to ensure connection is actually working
                _context.Database.ExecuteSqlRaw("SELECT 1");
                
                return new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    service = "SMHFR Backend API",
                    database = "connected"
                };
            }
            else
            {
                _logger.LogWarning("Database health check failed: Cannot connect to database");
                return new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    service = "SMHFR Backend API",
                    database = "disconnected"
                };
            }
        }
        catch (NpgsqlException npgsqlEx)
        {
            // Check for password authentication errors
            if (npgsqlEx.SqlState == "28P01")
            {
                _logger.LogError(npgsqlEx, "Database password authentication failed during health check");
                
                // Try to clear connection pool
                try
                {
                    NpgsqlConnection.ClearAllPools();
                    _logger.LogInformation("Cleared Npgsql connection pool due to authentication failure");
                }
                catch (Exception clearEx)
                {
                    _logger.LogError(clearEx, "Failed to clear connection pool");
                }
            }
            
            _logger.LogError(npgsqlEx, "Database health check failed with Npgsql exception: {SqlState}", npgsqlEx.SqlState);
            return new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                service = "SMHFR Backend API",
                database = "error",
                error = npgsqlEx.SqlState == "28P01" ? "authentication_failed" : "connection_error",
                message = npgsqlEx.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed with exception");
            return new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                service = "SMHFR Backend API",
                database = "error",
                error = ex.GetType().Name,
                message = ex.Message
            };
        }
    }
}
