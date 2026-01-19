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
        // Simple health check - just verify API is running
        // DO NOT check database here to avoid connection pool issues
        // - Health check runs every 15 seconds (docker-compose)
        // - Database checks can exhaust connection pool
        // - If DB connection fails, container will crash (caught by deployment script)
        // - Actual API requests will verify DB connectivity when needed
        return new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "SMHFR Backend API",
            message = "Service is running"
        };
    }
}
