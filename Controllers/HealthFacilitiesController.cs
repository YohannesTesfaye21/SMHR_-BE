using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMHFR_BE.Data;
using SMHFR_BE.DTOs;
using SMHFR_BE.Models;
using SMHFR_BE.Services;
using Npgsql;

namespace SMHFR_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthFacilitiesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HealthFacilitiesController> _logger;
    private readonly IValidationService _validationService;

    public HealthFacilitiesController(
        ApplicationDbContext context, 
        ILogger<HealthFacilitiesController> logger,
        IValidationService validationService)
    {
        _context = context;
        _logger = logger;
        _validationService = validationService;
    }

    /// <summary>
    /// Get all health facilities with optional pagination and filtering
    /// </summary>
    /// <param name="facilityName">Search by facility name (partial match, case-insensitive)</param>
    /// <param name="facilityTypeId">Filter by facility type ID</param>
    /// <param name="stateId">Filter by state ID</param>
    /// <param name="regionId">Filter by region ID</param>
    /// <param name="districtId">Filter by district ID</param>
    /// <param name="ownershipId">Filter by ownership ID</param>
    /// <param name="operationalStatusId">Filter by operational status ID</param>
    /// <param name="sortBy">Sort by field: "name", "createdAt", "updatedAt", "hcprojectenddate", "damalcaafimaadprojectenddate", "betterlifeprojectenddate", "caafimaadplusprojectenddate", or "id" (default: "id")</param>
    /// <param name="sortOrder">Sort order: "asc" or "desc" (default: "asc")</param>
    /// <param name="pageNumber">Page number (optional - if not provided, returns all results)</param>
    /// <param name="pageSize">Number of items per page (optional - if not provided, returns all results, max: 100)</param>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiPagedResponse<HealthFacilityDTO>>> GetHealthFacilities(
        [FromQuery] string? facilityName = null,
        [FromQuery] int? facilityTypeId = null,
        [FromQuery] int? stateId = null,
        [FromQuery] int? regionId = null,
        [FromQuery] int? districtId = null,
        [FromQuery] int? ownershipId = null,
        [FromQuery] int? operationalStatusId = null,
        [FromQuery] string sortBy = "id",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        try
        {
            // Determine if pagination is requested
            bool usePagination = pageNumber.HasValue && pageSize.HasValue;
            
            // Validate pagination parameters if provided
            int validatedPageNumber = 1;
            int validatedPageSize = 50;
            
            if (usePagination)
            {
                // Both values are guaranteed to be non-null here due to usePagination check
                validatedPageNumber = pageNumber!.Value < 1 ? 1 : pageNumber.Value;
                validatedPageSize = pageSize!.Value < 1 ? 50 : pageSize.Value;
                if (validatedPageSize > 100) validatedPageSize = 100; // Max page size
            }

            var query = _context.HealthFacilities
                .Include(hf => hf.District)
                    .ThenInclude(d => d.Region)
                        .ThenInclude(r => r.State)
                .Include(hf => hf.FacilityType)
                .Include(hf => hf.Ownership)
                .Include(hf => hf.OperationalStatus)
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
            if (ownershipId.HasValue)
                query = query.Where(hf => hf.OwnershipId == ownershipId.Value);

            // Filter by operational status
            if (operationalStatusId.HasValue)
                query = query.Where(hf => hf.OperationalStatusId == operationalStatusId.Value);

            // Apply sorting
            sortBy = sortBy?.Trim().ToLower() ?? "id";
            sortOrder = sortOrder?.Trim().ToLower() ?? "asc";
            bool isDescending = sortOrder == "desc";

            query = sortBy switch
            {
                "name" => isDescending 
                    ? query.OrderByDescending(hf => hf.HealthFacilityName)
                    : query.OrderBy(hf => hf.HealthFacilityName),
                "createdat" => isDescending
                    ? query.OrderByDescending(hf => hf.CreatedAt)
                    : query.OrderBy(hf => hf.CreatedAt),
                "updatedat" => isDescending
                    ? query.OrderByDescending(hf => hf.UpdatedAt)
                    : query.OrderBy(hf => hf.UpdatedAt),
                "hcprojectenddate" => isDescending
                    ? query.OrderByDescending(hf => hf.HCProjectEndDate ?? DateTime.MinValue)
                    : query.OrderBy(hf => hf.HCProjectEndDate ?? DateTime.MaxValue),
                "damalcaafimaadprojectenddate" => isDescending
                    ? query.OrderByDescending(hf => hf.DamalCaafimaadProjectEndDate ?? DateTime.MinValue)
                    : query.OrderBy(hf => hf.DamalCaafimaadProjectEndDate ?? DateTime.MaxValue),
                "betterlifeprojectenddate" => isDescending
                    ? query.OrderByDescending(hf => hf.BetterLifeProjectEndDate ?? DateTime.MinValue)
                    : query.OrderBy(hf => hf.BetterLifeProjectEndDate ?? DateTime.MaxValue),
                "caafimaadplusprojectenddate" => isDescending
                    ? query.OrderByDescending(hf => hf.CaafimaadPlusProjectEndDate ?? DateTime.MinValue)
                    : query.OrderBy(hf => hf.CaafimaadPlusProjectEndDate ?? DateTime.MaxValue),
                "id" or _ => isDescending
                    ? query.OrderByDescending(hf => hf.HealthFacilityId)
                    : query.OrderBy(hf => hf.HealthFacilityId)
            };

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination only if requested
            List<HealthFacility> facilities;
            if (usePagination)
            {
                facilities = await query
                    .Skip((validatedPageNumber - 1) * validatedPageSize)
                    .Take(validatedPageSize)
                    .ToListAsync();
            }
            else
            {
                // Return all results when pagination is not requested
                facilities = await query.ToListAsync();
                validatedPageNumber = 1;
                validatedPageSize = totalCount; // Set pageSize to totalCount to represent "all items on one page"
            }

            var facilityDTOs = facilities.Select(f => f.ToDTO()).ToList();
            
            var pagedResponse = new PagedResponse<HealthFacilityDTO>(
                facilityDTOs,
                totalCount,
                validatedPageNumber,
                validatedPageSize
            );
            
            return Ok(ApiPagedResponse<HealthFacilityDTO>.SuccessResult(pagedResponse, "Health facilities retrieved successfully"));
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "28P01")
        {
            _logger.LogError(pgEx, "❌ Database password authentication failed in GetHealthFacilities");
            _logger.LogError("Connection string used: {ConnectionString}", _context.Database.GetConnectionString()?.Replace("Password=postgres", "Password=***"));
            
            // Clear connection pool to force new connections
            try
            {
                Npgsql.NpgsqlConnection.ClearAllPools();
                _logger.LogInformation("Cleared Npgsql connection pool after authentication failure in GetHealthFacilities");
            }
            catch (Exception clearEx)
            {
                _logger.LogError(clearEx, "Failed to clear connection pool");
            }
            
            return StatusCode(500, ApiPagedResponse<HealthFacilityDTO>.ErrorResult(
                "Database authentication failed. Please check database credentials.",
                new List<string> 
                { 
                    "28P01: password authentication failed for user \"postgres\"",
                    "This error occurs when the PostgreSQL password in docker-compose.yml doesn't match the password stored in the database volume."
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health facilities: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
            
            // Include inner exception details if present
            var errorMessages = new List<string> { ex.Message };
            if (ex.InnerException != null)
            {
                errorMessages.Add($"Inner exception: {ex.InnerException.Message}");
            }
            
            return StatusCode(500, ApiPagedResponse<HealthFacilityDTO>.ErrorResult("An error occurred while retrieving health facilities", errorMessages));
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
                .Include(hf => hf.Ownership)
                .Include(hf => hf.OperationalStatus)
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
                .Include(hf => hf.Ownership)
                .Include(hf => hf.OperationalStatus)
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
    public async Task<ActionResult<ApiResponse<HealthFacilityDTO>>> CreateHealthFacility([FromBody] CreateHealthFacilityDTO dto)
    {
        try
        {
            // Validate using validation service
            var validationResult = await _validationService.ValidateCreateHealthFacilityAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(ApiResponse<HealthFacilityDTO>.ValidationErrorResult(validationResult, "Validation failed"));
            }

            var healthFacility = new HealthFacility
            {
                FacilityId = dto.FacilityId,
                HealthFacilityName = dto.HealthFacilityName,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DistrictId = dto.DistrictId,
                FacilityTypeId = dto.FacilityTypeId,
                OwnershipId = dto.OwnershipId,
                OperationalStatusId = dto.OperationalStatusId,
                HCPartners = dto.HCPartners,
                HCProjectEndDate = ToUtcDateTime(dto.HCProjectEndDate),
                NutritionClusterPartners = dto.NutritionClusterPartners,
                DamalCaafimaadPartner = dto.DamalCaafimaadPartner,
                DamalCaafimaadProjectEndDate = ToUtcDateTime(dto.DamalCaafimaadProjectEndDate),
                BetterLifeProjectPartner = dto.BetterLifeProjectPartner,
                BetterLifeProjectEndDate = ToUtcDateTime(dto.BetterLifeProjectEndDate),
                CaafimaadPlusPartner = dto.CaafimaadPlusPartner,
                CaafimaadPlusProjectEndDate = ToUtcDateTime(dto.CaafimaadPlusProjectEndDate),
                FacilityInChargeName = dto.FacilityInChargeName,
                FacilityInChargeNumber = dto.FacilityInChargeNumber,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

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
            await _context.Entry(healthFacility)
                .Reference(hf => hf.Ownership).LoadAsync();
            await _context.Entry(healthFacility)
                .Reference(hf => hf.OperationalStatus).LoadAsync();

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
    /// <param name="id">The health facility ID (from URL path)</param>
    /// <param name="dto">Update health facility data (only updatable fields, excludes healthFacilityId, createdAt, updatedAt, and navigation properties)</param>
    /// <returns>Updated health facility</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<HealthFacilityDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HealthFacilityDTO>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<HealthFacilityDTO>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<HealthFacilityDTO>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<HealthFacilityDTO>>> UpdateHealthFacility(int id, [FromBody] UpdateHealthFacilityDTO dto)
    {
        try
        {
            // Validate using validation service
            var validationResult = await _validationService.ValidateUpdateHealthFacilityAsync(id, dto);
            if (!validationResult.IsValid)
            {
                // Check if it's a "not found" error (should return 404)
                var notFoundError = validationResult.Errors.FirstOrDefault(e => e.Message.Contains("does not exist"));
                if (notFoundError != null)
                {
                    return NotFound(ApiResponse<HealthFacilityDTO>.ValidationErrorResult(validationResult, "Resource not found"));
                }
                // Other validation errors return 400
                return BadRequest(ApiResponse<HealthFacilityDTO>.ValidationErrorResult(validationResult, "Validation failed"));
            }

            // Get existing entity
            var healthFacility = await _context.HealthFacilities.FindAsync(id);
            if (healthFacility == null)
            {
                return NotFound(ApiResponse<HealthFacilityDTO>.ErrorResult($"Health facility with ID {id} not found"));
            }

            // Update properties from DTO
            healthFacility.FacilityId = dto.FacilityId;
            healthFacility.HealthFacilityName = dto.HealthFacilityName;
            healthFacility.Latitude = dto.Latitude;
            healthFacility.Longitude = dto.Longitude;
            healthFacility.DistrictId = dto.DistrictId;
            healthFacility.FacilityTypeId = dto.FacilityTypeId;
            healthFacility.OwnershipId = dto.OwnershipId;
            healthFacility.OperationalStatusId = dto.OperationalStatusId;
            healthFacility.HCPartners = dto.HCPartners;
            healthFacility.HCProjectEndDate = ToUtcDateTime(dto.HCProjectEndDate);
            healthFacility.NutritionClusterPartners = dto.NutritionClusterPartners;
            healthFacility.DamalCaafimaadPartner = dto.DamalCaafimaadPartner;
            healthFacility.DamalCaafimaadProjectEndDate = ToUtcDateTime(dto.DamalCaafimaadProjectEndDate);
            healthFacility.BetterLifeProjectPartner = dto.BetterLifeProjectPartner;
            healthFacility.BetterLifeProjectEndDate = ToUtcDateTime(dto.BetterLifeProjectEndDate);
            healthFacility.CaafimaadPlusPartner = dto.CaafimaadPlusPartner;
            healthFacility.CaafimaadPlusProjectEndDate = ToUtcDateTime(dto.CaafimaadPlusProjectEndDate);
            healthFacility.FacilityInChargeName = dto.FacilityInChargeName;
            healthFacility.FacilityInChargeNumber = dto.FacilityInChargeNumber;
            healthFacility.UpdatedAt = DateTime.UtcNow;

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
            await _context.Entry(healthFacility)
                .Reference(hf => hf.Ownership).LoadAsync();
            await _context.Entry(healthFacility)
                .Reference(hf => hf.OperationalStatus).LoadAsync();

            var response = ApiResponse<HealthFacilityDTO>.SuccessResult(
                healthFacility.ToDTO(),
                "Health facility updated successfully"
            );

            return Ok(response);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating health facility with ID {Id}", id);
            return StatusCode(500, ApiResponse<HealthFacilityDTO>.ErrorResult("An error occurred while updating the health facility. The record may have been modified by another process.", new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating health facility with ID {Id}", id);
            return StatusCode(500, ApiResponse<HealthFacilityDTO>.ErrorResult("An error occurred while updating the health facility", new List<string> { ex.Message }));
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
            // Validate using validation service
            var validationResult = await _validationService.ValidateDeleteHealthFacilityAsync(id);
            if (!validationResult.IsValid)
            {
                // Check if it's a "not found" error (should return 404)
                var notFoundError = validationResult.Errors.FirstOrDefault(e => e.Message.Contains("does not exist"));
                if (notFoundError != null)
                {
                    return NotFound(ApiResponse.ValidationErrorResult(validationResult, "Resource not found"));
                }
                // Other validation errors return 400
                return BadRequest(ApiResponse.ValidationErrorResult(validationResult, "Validation failed"));
            }

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

    /// <summary>
    /// Delete all health facilities
    /// </summary>
    [HttpDelete("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAllHealthFacilities()
    {
        try
        {
            // Get count before deletion for response
            var totalCount = await _context.HealthFacilities.CountAsync();

            if (totalCount == 0)
            {
                return Ok(ApiResponse<object>.SuccessResult(
                    new { deletedCount = 0, message = "No health facilities found to delete" },
                    "No health facilities were deleted. The database is already empty."
                ));
            }

            // Delete all health facilities
            var allFacilities = await _context.HealthFacilities.ToListAsync();
            _context.HealthFacilities.RemoveRange(allFacilities);
            await _context.SaveChangesAsync();

            _logger.LogWarning("⚠️  All {Count} health facilities have been deleted by admin", totalCount);

            return Ok(ApiResponse<object>.SuccessResult(
                new { deletedCount = totalCount, message = $"Successfully deleted {totalCount} health facility(ies)" },
                $"All {totalCount} health facility(ies) deleted successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all health facilities");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while deleting all health facilities", new List<string> { ex.Message }));
        }
    }

    private bool HealthFacilityExists(int id)
    {
        return _context.HealthFacilities.Any(e => e.HealthFacilityId == id);
    }

    /// <summary>
    /// Converts a DateTime to UTC for PostgreSQL compatibility.
    /// PostgreSQL requires UTC DateTime for timestamp with time zone columns.
    /// </summary>
    private static DateTime? ToUtcDateTime(DateTime? dateTime)
    {
        if (dateTime == null)
            return null;

        var dt = dateTime.Value;
        
        // If Kind is Unspecified (common when deserializing from JSON), treat as UTC
        if (dt.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }
        
        // If already UTC, return as is
        if (dt.Kind == DateTimeKind.Utc)
        {
            return dt;
        }
        
        // If Local, convert to UTC
        return dt.ToUniversalTime();
    }
}
