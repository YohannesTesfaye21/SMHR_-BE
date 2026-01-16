using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMHFR_BE.Data;
using SMHFR_BE.DTOs;
using SMHFR_BE.Models;
using SMHFR_BE.Services;

namespace SMHFR_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthFacilitiesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HealthFacilitiesController> _logger;

    public HealthFacilitiesController(ApplicationDbContext context, ILogger<HealthFacilitiesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all health facilities with pagination and filtering
    /// </summary>
    /// <param name="facilityName">Search by facility name (partial match, case-insensitive)</param>
    /// <param name="facilityTypeId">Filter by facility type ID</param>
    /// <param name="stateId">Filter by state ID</param>
    /// <param name="regionId">Filter by region ID</param>
    /// <param name="districtId">Filter by district ID</param>
    /// <param name="ownership">Filter by ownership</param>
    /// <param name="operationalStatus">Filter by operational status</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 50, max: 100)</param>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiPagedResponse<HealthFacilityDTO>>> GetHealthFacilities(
        [FromQuery] string? facilityName = null,
        [FromQuery] int? facilityTypeId = null,
        [FromQuery] int? stateId = null,
        [FromQuery] int? regionId = null,
        [FromQuery] int? districtId = null,
        [FromQuery] string? ownership = null,
        [FromQuery] string? operationalStatus = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100; // Max page size

            var query = _context.HealthFacilities
                .Include(hf => hf.District)
                    .ThenInclude(d => d.Region)
                        .ThenInclude(r => r.State)
                .Include(hf => hf.FacilityType)
                .AsQueryable();

            // Filter by facility name (partial match, case-insensitive)
            if (!string.IsNullOrWhiteSpace(facilityName))
            {
                var searchTerm = facilityName.Trim();
                query = query.Where(hf => hf.HealthFacilityName.ToLower().Contains(searchTerm.ToLower()));
            }

            // Filter by facility type
            if (facilityTypeId.HasValue)
                query = query.Where(hf => hf.FacilityTypeId == facilityTypeId.Value);

            // Filter by state
            if (stateId.HasValue)
                query = query.Where(hf => hf.District.Region.State.StateId == stateId.Value);

            // Filter by region
            if (regionId.HasValue)
                query = query.Where(hf => hf.District.Region.RegionId == regionId.Value);

            // Filter by district
            if (districtId.HasValue)
                query = query.Where(hf => hf.DistrictId == districtId.Value);

            // Filter by ownership
            if (!string.IsNullOrWhiteSpace(ownership))
                query = query.Where(hf => hf.Ownership == ownership.Trim());

            // Filter by operational status
            if (!string.IsNullOrWhiteSpace(operationalStatus))
                query = query.Where(hf => hf.OperationalStatus == operationalStatus.Trim());

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var facilities = await query
                .OrderBy(hf => hf.HealthFacilityId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var facilityDTOs = facilities.Select(f => f.ToDTO()).ToList();
            
            var pagedResponse = new PagedResponse<HealthFacilityDTO>(
                facilityDTOs,
                totalCount,
                pageNumber,
                pageSize
            );
            
            return Ok(ApiPagedResponse<HealthFacilityDTO>.SuccessResult(pagedResponse, "Health facilities retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health facilities");
            return StatusCode(500, ApiPagedResponse<HealthFacilityDTO>.ErrorResult("An error occurred while retrieving health facilities", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Get health facility by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<HealthFacilityDTO>>> GetHealthFacility(int id)
    {
        try
        {
            var healthFacility = await _context.HealthFacilities
                .Include(hf => hf.District)
                    .ThenInclude(d => d.Region)
                        .ThenInclude(r => r.State)
                .Include(hf => hf.FacilityType)
                .FirstOrDefaultAsync(hf => hf.HealthFacilityId == id);

            if (healthFacility == null)
            {
                return NotFound(ApiResponse<HealthFacilityDTO>.ErrorResult($"Health facility with ID {id} not found"));
            }

            return Ok(ApiResponse<HealthFacilityDTO>.SuccessResult(healthFacility.ToDTO(), "Health facility retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health facility with ID {Id}", id);
            return StatusCode(500, ApiResponse<HealthFacilityDTO>.ErrorResult("An error occurred while retrieving the health facility", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Get health facility by Facility ID
    /// </summary>
    [HttpGet("facility-id/{facilityId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<HealthFacilityDTO>>> GetHealthFacilityByFacilityId(string facilityId)
    {
        try
        {
            var healthFacility = await _context.HealthFacilities
                .Include(hf => hf.District)
                    .ThenInclude(d => d.Region)
                        .ThenInclude(r => r.State)
                .Include(hf => hf.FacilityType)
                .FirstOrDefaultAsync(hf => hf.FacilityId == facilityId);

            if (healthFacility == null)
            {
                return NotFound(ApiResponse<HealthFacilityDTO>.ErrorResult($"Health facility with Facility ID '{facilityId}' not found"));
            }

            return Ok(ApiResponse<HealthFacilityDTO>.SuccessResult(healthFacility.ToDTO(), "Health facility retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health facility with Facility ID {FacilityId}", facilityId);
            return StatusCode(500, ApiResponse<HealthFacilityDTO>.ErrorResult("An error occurred while retrieving the health facility", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Create a new health facility
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<HealthFacilityDTO>>> CreateHealthFacility(HealthFacility healthFacility)
    {
        try
        {
            healthFacility.CreatedAt = DateTime.UtcNow;
            healthFacility.UpdatedAt = DateTime.UtcNow;

            _context.HealthFacilities.Add(healthFacility);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            await _context.Entry(healthFacility)
                .Reference(hf => hf.District).LoadAsync();
            await _context.Entry(healthFacility.District)
                .Reference(d => d.Region).LoadAsync();
            await _context.Entry(healthFacility.District.Region)
                .Reference(r => r.State).LoadAsync();
            await _context.Entry(healthFacility)
                .Reference(hf => hf.FacilityType).LoadAsync();

            var response = ApiResponse<HealthFacilityDTO>.SuccessResult(
                healthFacility.ToDTO(),
                "Health facility created successfully"
            );

            return CreatedAtAction(nameof(GetHealthFacility), new { id = healthFacility.HealthFacilityId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating health facility");
            return StatusCode(500, ApiResponse<HealthFacilityDTO>.ErrorResult("An error occurred while creating the health facility", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Update a health facility
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> UpdateHealthFacility(int id, HealthFacility healthFacility)
    {
        try
        {
            if (id != healthFacility.HealthFacilityId)
            {
                return BadRequest(ApiResponse.ErrorResult("ID in URL does not match ID in request body"));
            }

            if (!HealthFacilityExists(id))
            {
                return NotFound(ApiResponse.ErrorResult($"Health facility with ID {id} not found"));
            }

            healthFacility.UpdatedAt = DateTime.UtcNow;
            _context.Entry(healthFacility).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult("Health facility updated successfully"));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating health facility with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while updating the health facility. The record may have been modified by another process.", new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating health facility with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while updating the health facility", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Delete a health facility
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteHealthFacility(int id)
    {
        try
        {
            var healthFacility = await _context.HealthFacilities.FindAsync(id);
            if (healthFacility == null)
            {
                return NotFound(ApiResponse.ErrorResult($"Health facility with ID {id} not found"));
            }

            _context.HealthFacilities.Remove(healthFacility);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult("Health facility deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting health facility with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while deleting the health facility", new List<string> { ex.Message }));
        }
    }

    private bool HealthFacilityExists(int id)
    {
        return _context.HealthFacilities.Any(e => e.HealthFacilityId == id);
    }
}
