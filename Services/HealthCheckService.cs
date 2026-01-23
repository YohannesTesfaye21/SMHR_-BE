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
        // Lightweight health check that verifies database connectivity
        // Uses a simple query that doesn't lock tables or exhaust connections
        try
        {
            // Use a lightweight query that doesn't require table locks
            // This will fail fast if authentication is wrong (28P01)
            var canConnect = _context.Database.CanConnect();
            
            if (!canConnect)
            {
                return new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    service = "SMHFR Backend API",
                    message = "Database connection failed",
                    database = "disconnected"
                };
            }

            // Try a simple query to verify authentication works
            // Using ExecuteSqlRaw with a simple SELECT 1 that doesn't touch any tables
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
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "28P01")
        {
            // Password authentication failed - service is unhealthy
            _logger.LogError(ex, "❌ Database password authentication failed in health check");
            return new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                service = "SMHFR Backend API",
                message = "Database authentication failed",
                database = "authentication_error",
                error = "28P01: password authentication failed"
            };
        }
        catch (Exception ex)
        {
            // Other database errors - log but don't fail health check
            // This allows the service to start even if DB is temporarily unavailable
            _logger.LogWarning(ex, "⚠️  Database health check failed, but service is still running");
            return new
            {
                status = "degraded",
                timestamp = DateTime.UtcNow,
                service = "SMHFR Backend API",
                message = "Service is running but database may be unavailable",
                database = "error",
                error = ex.Message
            };
        }
    }
}
