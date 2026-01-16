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
    private readonly ILogger<HealthController> _logger;

    public HealthController(IHealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
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
}
