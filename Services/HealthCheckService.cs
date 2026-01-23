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
        // Health check that always returns a response (never throws)
        // API should be accessible even if database is down
        try
        {
            // Try to check database connectivity (non-blocking, with timeout)
            var canConnect = false;
            try
            {
                // Use CanConnect with a short timeout to avoid blocking
                canConnect = _context.Database.CanConnect();
                
                if (canConnect)
                {
                    // Try a simple query to verify authentication works
                    _context.Database.ExecuteSqlRaw("SELECT 1");
                    
                    return new
                    {
                        status = "healthy",
                        timestamp = DateTime.UtcNow,
                        service = "SMHFR Backend API",
                        message = "Service is running",
                        database = "connected"
                    };
                }
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "28P01")
            {
                // Password authentication failed
                _logger.LogError(ex, "❌ Database password authentication failed in health check");
                return new
                {
                    status = "degraded",
                    timestamp = DateTime.UtcNow,
                    service = "SMHFR Backend API",
                    message = "Service is running but database authentication failed",
                    database = "authentication_error",
                    error = "28P01: password authentication failed"
                };
            }
            catch (Exception dbEx)
            {
                // Other database errors - service is still running
                _logger.LogWarning(dbEx, "⚠️  Database health check failed, but service is still running");
                return new
                {
                    status = "degraded",
                    timestamp = DateTime.UtcNow,
                    service = "SMHFR Backend API",
                    message = "Service is running but database may be unavailable",
                    database = "error",
                    error = dbEx.Message
                };
            }
            
            // If we get here, CanConnect returned false
            return new
            {
                status = "degraded",
                timestamp = DateTime.UtcNow,
                service = "SMHFR Backend API",
                message = "Service is running but database connection failed",
                database = "disconnected"
            };
        }
        catch (Exception ex)
        {
            // Catch-all to ensure health check never crashes the app
            _logger.LogError(ex, "❌ Unexpected error in health check");
            return new
            {
                status = "degraded",
                timestamp = DateTime.UtcNow,
                service = "SMHFR Backend API",
                message = "Service is running but health check encountered an error",
                error = ex.Message
            };
        }
    }
}
