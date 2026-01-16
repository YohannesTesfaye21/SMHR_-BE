namespace SMHFR_BE.Services;

public class HealthCheckService : IHealthCheckService
{
    public object CheckHealth()
    {
        return new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "SMHFR Backend API"
        };
    }
}
