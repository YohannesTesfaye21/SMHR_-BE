using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMHFR_BE.Data;
using SMHFR_BE.DTOs;
using SMHFR_BE.Models;

namespace SMHFR_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LookupTablesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LookupTablesController> _logger;

    public LookupTablesController(ApplicationDbContext context, ILogger<LookupTablesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all states with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 50, max: 100)</param>
    [HttpGet("states")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiPagedResponse<State>>> GetStates(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100;

            var query = _context.States.AsQueryable();

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var states = await query
                .OrderBy(s => s.StateName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedResponse = new PagedResponse<State>(states, totalCount, pageNumber, pageSize);
            return Ok(ApiPagedResponse<State>.SuccessResult(pagedResponse, "States retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving states");
            return StatusCode(500, ApiPagedResponse<State>.ErrorResult("An error occurred while retrieving states", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Get regions by state with pagination
    /// </summary>
    /// <param name="stateId">Filter by state ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 50, max: 100)</param>
    [HttpGet("regions")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiPagedResponse<Region>>> GetRegions(
        [FromQuery] int? stateId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Regions
                .Include(r => r.State)
                .AsQueryable();

            if (stateId.HasValue)
                query = query.Where(r => r.StateId == stateId.Value);

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var regions = await query
                .OrderBy(r => r.RegionName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedResponse = new PagedResponse<Region>(regions, totalCount, pageNumber, pageSize);
            return Ok(ApiPagedResponse<Region>.SuccessResult(pagedResponse, "Regions retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving regions");
            return StatusCode(500, ApiPagedResponse<Region>.ErrorResult("An error occurred while retrieving regions", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Get districts by region with pagination
    /// </summary>
    /// <param name="regionId">Filter by region ID</param>
    /// <param name="stateId">Filter by state ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 50, max: 100)</param>
    [HttpGet("districts")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiPagedResponse<District>>> GetDistricts(
        [FromQuery] int? regionId = null,
        [FromQuery] int? stateId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Districts
                .Include(d => d.Region)
                    .ThenInclude(r => r.State)
                .AsQueryable();

            if (regionId.HasValue)
                query = query.Where(d => d.RegionId == regionId.Value);

            if (stateId.HasValue)
                query = query.Where(d => d.Region.StateId == stateId.Value);

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var districts = await query
                .OrderBy(d => d.DistrictName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedResponse = new PagedResponse<District>(districts, totalCount, pageNumber, pageSize);
            return Ok(ApiPagedResponse<District>.SuccessResult(pagedResponse, "Districts retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving districts");
            return StatusCode(500, ApiPagedResponse<District>.ErrorResult("An error occurred while retrieving districts", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Get all facility types with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 50, max: 100)</param>
    [HttpGet("facility-types")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiPagedResponse<FacilityType>>> GetFacilityTypes(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100;

            var query = _context.FacilityTypes.AsQueryable();

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var facilityTypes = await query
                .OrderBy(ft => ft.TypeName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedResponse = new PagedResponse<FacilityType>(facilityTypes, totalCount, pageNumber, pageSize);
            return Ok(ApiPagedResponse<FacilityType>.SuccessResult(pagedResponse, "Facility types retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving facility types");
            return StatusCode(500, ApiPagedResponse<FacilityType>.ErrorResult("An error occurred while retrieving facility types", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Get all operational statuses with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 50, max: 100)</param>
    [HttpGet("operational-statuses")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiPagedResponse<OperationalStatus>>> GetOperationalStatuses(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100;

            var query = _context.OperationalStatuses.AsQueryable();

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var operationalStatuses = await query
                .OrderBy(os => os.StatusName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedResponse = new PagedResponse<OperationalStatus>(operationalStatuses, totalCount, pageNumber, pageSize);
            return Ok(ApiPagedResponse<OperationalStatus>.SuccessResult(pagedResponse, "Operational statuses retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving operational statuses");
            return StatusCode(500, ApiPagedResponse<OperationalStatus>.ErrorResult("An error occurred while retrieving operational statuses", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Get all ownerships with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 50, max: 100)</param>
    [HttpGet("ownerships")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiPagedResponse<Ownership>>> GetOwnerships(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Ownerships.AsQueryable();

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var ownerships = await query
                .OrderBy(o => o.OwnershipType)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedResponse = new PagedResponse<Ownership>(ownerships, totalCount, pageNumber, pageSize);
            return Ok(ApiPagedResponse<Ownership>.SuccessResult(pagedResponse, "Ownerships retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ownerships");
            return StatusCode(500, ApiPagedResponse<Ownership>.ErrorResult("An error occurred while retrieving ownerships", new List<string> { ex.Message }));
        }
    }

    // ==================== STATE CRUD ====================

    /// <summary>
    /// Create a new state
    /// </summary>
    [HttpPost("states")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<State>>> CreateState(State state)
    {
        try
        {
            if (await _context.States.AnyAsync(s => s.StateCode == state.StateCode))
            {
                return BadRequest(ApiResponse<State>.ErrorResult($"State with code '{state.StateCode}' already exists"));
            }

            state.CreatedAt = DateTime.UtcNow;
            _context.States.Add(state);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStates), new { }, ApiResponse<State>.SuccessResult(state, "State created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating state");
            return StatusCode(500, ApiResponse<State>.ErrorResult("An error occurred while creating the state", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Update a state
    /// </summary>
    [HttpPut("states/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> UpdateState(int id, State state)
    {
        try
        {
            if (id != state.StateId)
            {
                return BadRequest(ApiResponse.ErrorResult("ID in URL does not match ID in request body"));
            }

            var existingState = await _context.States.FindAsync(id);
            if (existingState == null)
            {
                return NotFound(ApiResponse.ErrorResult($"State with ID {id} not found"));
            }

            // Check if StateCode is being changed and if new code already exists
            if (existingState.StateCode != state.StateCode && await _context.States.AnyAsync(s => s.StateCode == state.StateCode))
            {
                return BadRequest(ApiResponse.ErrorResult($"State with code '{state.StateCode}' already exists"));
            }

            existingState.StateCode = state.StateCode;
            existingState.StateName = state.StateName;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult("State updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating state with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while updating the state", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Delete a state
    /// </summary>
    [HttpDelete("states/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteState(int id)
    {
        try
        {
            var state = await _context.States.Include(s => s.Regions).FirstOrDefaultAsync(s => s.StateId == id);
            if (state == null)
            {
                return NotFound(ApiResponse.ErrorResult($"State with ID {id} not found"));
            }

            if (state.Regions.Any())
            {
                return BadRequest(ApiResponse.ErrorResult("Cannot delete state because it has associated regions. Please delete all regions first."));
            }

            _context.States.Remove(state);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult("State deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting state with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while deleting the state", new List<string> { ex.Message }));
        }
    }

    // ==================== REGION CRUD ====================

    /// <summary>
    /// Create a new region
    /// </summary>
    [HttpPost("regions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<Region>>> CreateRegion(Region region)
    {
        try
        {
            if (!await _context.States.AnyAsync(s => s.StateId == region.StateId))
            {
                return BadRequest(ApiResponse<Region>.ErrorResult($"State with ID {region.StateId} does not exist"));
            }

            if (await _context.Regions.AnyAsync(r => r.StateId == region.StateId && r.RegionName == region.RegionName))
            {
                return BadRequest(ApiResponse<Region>.ErrorResult($"Region with name '{region.RegionName}' already exists in this state"));
            }

            region.CreatedAt = DateTime.UtcNow;
            _context.Regions.Add(region);
            await _context.SaveChangesAsync();

            await _context.Entry(region).Reference(r => r.State).LoadAsync();

            return CreatedAtAction(nameof(GetRegions), new { }, ApiResponse<Region>.SuccessResult(region, "Region created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating region");
            return StatusCode(500, ApiResponse<Region>.ErrorResult("An error occurred while creating the region", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Update a region
    /// </summary>
    [HttpPut("regions/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> UpdateRegion(int id, Region region)
    {
        try
        {
            if (id != region.RegionId)
            {
                return BadRequest(ApiResponse.ErrorResult("ID in URL does not match ID in request body"));
            }

            var existingRegion = await _context.Regions.FindAsync(id);
            if (existingRegion == null)
            {
                return NotFound(ApiResponse.ErrorResult($"Region with ID {id} not found"));
            }

            if (!await _context.States.AnyAsync(s => s.StateId == region.StateId))
            {
                return BadRequest(ApiResponse.ErrorResult($"State with ID {region.StateId} does not exist"));
            }

            // Check if RegionName is being changed and if new name already exists in the state
            if ((existingRegion.StateId != region.StateId || existingRegion.RegionName != region.RegionName) 
                && await _context.Regions.AnyAsync(r => r.StateId == region.StateId && r.RegionName == region.RegionName && r.RegionId != id))
            {
                return BadRequest(ApiResponse.ErrorResult($"Region with name '{region.RegionName}' already exists in this state"));
            }

            existingRegion.StateId = region.StateId;
            existingRegion.RegionName = region.RegionName;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult("Region updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating region with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while updating the region", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Delete a region
    /// </summary>
    [HttpDelete("regions/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteRegion(int id)
    {
        try
        {
            var region = await _context.Regions.Include(r => r.Districts).FirstOrDefaultAsync(r => r.RegionId == id);
            if (region == null)
            {
                return NotFound(ApiResponse.ErrorResult($"Region with ID {id} not found"));
            }

            if (region.Districts.Any())
            {
                return BadRequest(ApiResponse.ErrorResult("Cannot delete region because it has associated districts. Please delete all districts first."));
            }

            _context.Regions.Remove(region);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult("Region deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting region with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while deleting the region", new List<string> { ex.Message }));
        }
    }

    // ==================== DISTRICT CRUD ====================

    /// <summary>
    /// Create a new district
    /// </summary>
    [HttpPost("districts")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<District>>> CreateDistrict(District district)
    {
        try
        {
            if (!await _context.Regions.AnyAsync(r => r.RegionId == district.RegionId))
            {
                return BadRequest(ApiResponse<District>.ErrorResult($"Region with ID {district.RegionId} does not exist"));
            }

            if (await _context.Districts.AnyAsync(d => d.RegionId == district.RegionId && d.DistrictName == district.DistrictName))
            {
                return BadRequest(ApiResponse<District>.ErrorResult($"District with name '{district.DistrictName}' already exists in this region"));
            }

            district.CreatedAt = DateTime.UtcNow;
            _context.Districts.Add(district);
            await _context.SaveChangesAsync();

            await _context.Entry(district).Reference(d => d.Region).LoadAsync();
            await _context.Entry(district.Region).Reference(r => r.State).LoadAsync();

            return CreatedAtAction(nameof(GetDistricts), new { }, ApiResponse<District>.SuccessResult(district, "District created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating district");
            return StatusCode(500, ApiResponse<District>.ErrorResult("An error occurred while creating the district", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Update a district
    /// </summary>
    [HttpPut("districts/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> UpdateDistrict(int id, District district)
    {
        try
        {
            if (id != district.DistrictId)
            {
                return BadRequest(ApiResponse.ErrorResult("ID in URL does not match ID in request body"));
            }

            var existingDistrict = await _context.Districts.FindAsync(id);
            if (existingDistrict == null)
            {
                return NotFound(ApiResponse.ErrorResult($"District with ID {id} not found"));
            }

            if (!await _context.Regions.AnyAsync(r => r.RegionId == district.RegionId))
            {
                return BadRequest(ApiResponse.ErrorResult($"Region with ID {district.RegionId} does not exist"));
            }

            // Check if DistrictName is being changed and if new name already exists in the region
            if ((existingDistrict.RegionId != district.RegionId || existingDistrict.DistrictName != district.DistrictName)
                && await _context.Districts.AnyAsync(d => d.RegionId == district.RegionId && d.DistrictName == district.DistrictName && d.DistrictId != id))
            {
                return BadRequest(ApiResponse.ErrorResult($"District with name '{district.DistrictName}' already exists in this region"));
            }

            existingDistrict.RegionId = district.RegionId;
            existingDistrict.DistrictName = district.DistrictName;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult("District updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating district with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while updating the district", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Delete a district
    /// </summary>
    [HttpDelete("districts/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteDistrict(int id)
    {
        try
        {
            var district = await _context.Districts.Include(d => d.HealthFacilities).FirstOrDefaultAsync(d => d.DistrictId == id);
            if (district == null)
            {
                return NotFound(ApiResponse.ErrorResult($"District with ID {id} not found"));
            }

            if (district.HealthFacilities.Any())
            {
                return BadRequest(ApiResponse.ErrorResult("Cannot delete district because it has associated health facilities. Please delete or reassign all health facilities first."));
            }

            _context.Districts.Remove(district);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult("District deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting district with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while deleting the district", new List<string> { ex.Message }));
        }
    }

    // ==================== FACILITY TYPE CRUD ====================

    /// <summary>
    /// Create a new facility type
    /// </summary>
    [HttpPost("facility-types")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<FacilityType>>> CreateFacilityType(FacilityType facilityType)
    {
        try
        {
            if (await _context.FacilityTypes.AnyAsync(ft => ft.TypeName == facilityType.TypeName))
            {
                return BadRequest(ApiResponse<FacilityType>.ErrorResult($"Facility type with name '{facilityType.TypeName}' already exists"));
            }

            facilityType.CreatedAt = DateTime.UtcNow;
            _context.FacilityTypes.Add(facilityType);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFacilityTypes), new { }, ApiResponse<FacilityType>.SuccessResult(facilityType, "Facility type created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating facility type");
            return StatusCode(500, ApiResponse<FacilityType>.ErrorResult("An error occurred while creating the facility type", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Update a facility type
    /// </summary>
    [HttpPut("facility-types/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> UpdateFacilityType(int id, FacilityType facilityType)
    {
        try
        {
            if (id != facilityType.FacilityTypeId)
            {
                return BadRequest(ApiResponse.ErrorResult("ID in URL does not match ID in request body"));
            }

            var existingFacilityType = await _context.FacilityTypes.FindAsync(id);
            if (existingFacilityType == null)
            {
                return NotFound(ApiResponse.ErrorResult($"Facility type with ID {id} not found"));
            }

            // Check if TypeName is being changed and if new name already exists
            if (existingFacilityType.TypeName != facilityType.TypeName && await _context.FacilityTypes.AnyAsync(ft => ft.TypeName == facilityType.TypeName))
            {
                return BadRequest(ApiResponse.ErrorResult($"Facility type with name '{facilityType.TypeName}' already exists"));
            }

            existingFacilityType.TypeName = facilityType.TypeName;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult("Facility type updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating facility type with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while updating the facility type", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Delete a facility type
    /// </summary>
    [HttpDelete("facility-types/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteFacilityType(int id)
    {
        try
        {
            var facilityType = await _context.FacilityTypes.Include(ft => ft.HealthFacilities).FirstOrDefaultAsync(ft => ft.FacilityTypeId == id);
            if (facilityType == null)
            {
                return NotFound(ApiResponse.ErrorResult($"Facility type with ID {id} not found"));
            }

            if (facilityType.HealthFacilities.Any())
            {
                return BadRequest(ApiResponse.ErrorResult("Cannot delete facility type because it has associated health facilities. Please delete or reassign all health facilities first."));
            }

            _context.FacilityTypes.Remove(facilityType);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult("Facility type deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting facility type with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while deleting the facility type", new List<string> { ex.Message }));
        }
    }

    // ==================== OPERATIONAL STATUS CRUD ====================

    /// <summary>
    /// Create a new operational status
    /// </summary>
    [HttpPost("operational-statuses")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<OperationalStatus>>> CreateOperationalStatus(OperationalStatus operationalStatus)
    {
        try
        {
            if (await _context.OperationalStatuses.AnyAsync(os => os.StatusName == operationalStatus.StatusName))
            {
                return BadRequest(ApiResponse<OperationalStatus>.ErrorResult($"Operational status with name '{operationalStatus.StatusName}' already exists"));
            }

            operationalStatus.CreatedAt = DateTime.UtcNow;
            _context.OperationalStatuses.Add(operationalStatus);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOperationalStatuses), new { }, ApiResponse<OperationalStatus>.SuccessResult(operationalStatus, "Operational status created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating operational status");
            return StatusCode(500, ApiResponse<OperationalStatus>.ErrorResult("An error occurred while creating the operational status", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Update an operational status
    /// </summary>
    [HttpPut("operational-statuses/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> UpdateOperationalStatus(int id, OperationalStatus operationalStatus)
    {
        try
        {
            if (id != operationalStatus.OperationalStatusId)
            {
                return BadRequest(ApiResponse.ErrorResult("ID in URL does not match ID in request body"));
            }

            var existingOperationalStatus = await _context.OperationalStatuses.FindAsync(id);
            if (existingOperationalStatus == null)
            {
                return NotFound(ApiResponse.ErrorResult($"Operational status with ID {id} not found"));
            }

            // Check if StatusName is being changed and if new name already exists
            if (existingOperationalStatus.StatusName != operationalStatus.StatusName && await _context.OperationalStatuses.AnyAsync(os => os.StatusName == operationalStatus.StatusName))
            {
                return BadRequest(ApiResponse.ErrorResult($"Operational status with name '{operationalStatus.StatusName}' already exists"));
            }

            existingOperationalStatus.StatusName = operationalStatus.StatusName;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult("Operational status updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating operational status with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while updating the operational status", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Delete an operational status
    /// </summary>
    [HttpDelete("operational-statuses/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteOperationalStatus(int id)
    {
        try
        {
            var operationalStatus = await _context.OperationalStatuses.Include(os => os.HealthFacilities).FirstOrDefaultAsync(os => os.OperationalStatusId == id);
            if (operationalStatus == null)
            {
                return NotFound(ApiResponse.ErrorResult($"Operational status with ID {id} not found"));
            }

            if (operationalStatus.HealthFacilities.Any())
            {
                return BadRequest(ApiResponse.ErrorResult("Cannot delete operational status because it has associated health facilities. Please delete or reassign all health facilities first."));
            }

            _context.OperationalStatuses.Remove(operationalStatus);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult("Operational status deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting operational status with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while deleting the operational status", new List<string> { ex.Message }));
        }
    }

    // ==================== OWNERSHIP CRUD ====================

    /// <summary>
    /// Create a new ownership
    /// </summary>
    [HttpPost("ownerships")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<Ownership>>> CreateOwnership(Ownership ownership)
    {
        try
        {
            if (await _context.Ownerships.AnyAsync(o => o.OwnershipType == ownership.OwnershipType))
            {
                return BadRequest(ApiResponse<Ownership>.ErrorResult($"Ownership with type '{ownership.OwnershipType}' already exists"));
            }

            ownership.CreatedAt = DateTime.UtcNow;
            _context.Ownerships.Add(ownership);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOwnerships), new { }, ApiResponse<Ownership>.SuccessResult(ownership, "Ownership created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ownership");
            return StatusCode(500, ApiResponse<Ownership>.ErrorResult("An error occurred while creating the ownership", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Update an ownership
    /// </summary>
    [HttpPut("ownerships/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> UpdateOwnership(int id, Ownership ownership)
    {
        try
        {
            if (id != ownership.OwnershipId)
            {
                return BadRequest(ApiResponse.ErrorResult("ID in URL does not match ID in request body"));
            }

            var existingOwnership = await _context.Ownerships.FindAsync(id);
            if (existingOwnership == null)
            {
                return NotFound(ApiResponse.ErrorResult($"Ownership with ID {id} not found"));
            }

            // Check if OwnershipType is being changed and if new type already exists
            if (existingOwnership.OwnershipType != ownership.OwnershipType && await _context.Ownerships.AnyAsync(o => o.OwnershipType == ownership.OwnershipType))
            {
                return BadRequest(ApiResponse.ErrorResult($"Ownership with type '{ownership.OwnershipType}' already exists"));
            }

            existingOwnership.OwnershipType = ownership.OwnershipType;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult("Ownership updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ownership with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while updating the ownership", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Delete an ownership
    /// </summary>
    [HttpDelete("ownerships/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteOwnership(int id)
    {
        try
        {
            var ownership = await _context.Ownerships.Include(o => o.HealthFacilities).FirstOrDefaultAsync(o => o.OwnershipId == id);
            if (ownership == null)
            {
                return NotFound(ApiResponse.ErrorResult($"Ownership with ID {id} not found"));
            }

            if (ownership.HealthFacilities.Any())
            {
                return BadRequest(ApiResponse.ErrorResult("Cannot delete ownership because it has associated health facilities. Please delete or reassign all health facilities first."));
            }

            _context.Ownerships.Remove(ownership);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult("Ownership deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ownership with ID {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while deleting the ownership", new List<string> { ex.Message }));
        }
    }
}
