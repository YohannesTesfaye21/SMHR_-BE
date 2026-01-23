using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMHFR_BE.DTOs;
using SMHFR_BE.Services;

namespace SMHFR_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly IDatabaseInitializationService _dbInitService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IHealthCheckService healthCheckService, 
        IDatabaseInitializationService dbInitService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _dbInitService = dbInitService;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        try
        {
            var status = _healthCheckService.CheckHealth();
            return Ok(ApiResponse<object>.SuccessResult(status, "Service is healthy"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health");
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while checking health", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Run database migrations manually (Admin only)
    /// </summary>
    [HttpPost("migrate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> RunMigrations()
    {
        try
        {
            _logger.LogInformation("🔄 Manual migration triggered by admin");
            await _dbInitService.InitializeAsync();
            
            return Ok(ApiResponse<object>.SuccessResult(
                new { message = "Database migrations applied successfully" },
                "Migrations completed successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to run migrations");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "Failed to apply database migrations",
                new List<string> { ex.Message, ex.InnerException?.Message ?? "" }.Where(s => !string.IsNullOrEmpty(s)).ToList()
            ));
        }
    }
}
