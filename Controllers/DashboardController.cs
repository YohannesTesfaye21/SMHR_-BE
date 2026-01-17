using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMHFR_BE.Data;
using SMHFR_BE.DTOs;

namespace SMHFR_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard card statistics (total states, regions, districts, facilities)
    /// </summary>
    [HttpGet("cards")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CardDataDTO>>> GetCardStatistics()
    {
        try
        {
            var totalStates = await _context.States.CountAsync();
            var totalRegions = await _context.Regions.CountAsync();
            var totalDistricts = await _context.Districts.CountAsync();
            var totalFacilities = await _context.HealthFacilities.CountAsync();

            var cardData = new CardDataDTO
            {
                TotalStates = totalStates,
                TotalRegions = totalRegions,
                TotalDistricts = totalDistricts,
                TotalFacilities = totalFacilities
            };

            return Ok(ApiResponse<CardDataDTO>.SuccessResult(cardData, "Card statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving card statistics");
            return StatusCode(500, ApiResponse<CardDataDTO>.ErrorResult("An error occurred while retrieving card statistics", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Get bar chart data for facilities by state, region, or facility type
    /// </summary>
    /// <param name="groupBy">Group by: 'state', 'region', or 'facilitytype'</param>
    /// <param name="stateId">Optional filter by state ID</param>
    /// <param name="regionId">Optional filter by region ID</param>
    /// <param name="facilityTypeId">Optional filter by facility type ID</param>
    [HttpGet("barchart")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<ChartDataDTO>>>> GetBarChartData(
        [FromQuery] string groupBy = "state",
        [FromQuery] int? stateId = null,
        [FromQuery] int? regionId = null,
        [FromQuery] int? facilityTypeId = null)
    {
        try
        {
            var query = _context.HealthFacilities.AsQueryable();

            // Apply filters
            if (stateId.HasValue)
            {
                query = query.Where(hf => hf.District.Region.StateId == stateId.Value);
            }

            if (regionId.HasValue)
            {
                query = query.Where(hf => hf.District.RegionId == regionId.Value);
            }

            if (facilityTypeId.HasValue)
            {
                query = query.Where(hf => hf.FacilityTypeId == facilityTypeId.Value);
            }

            List<ChartDataDTO> chartData;

            switch (groupBy.ToLower())
            {
                case "state":
                    chartData = await query
                        .GroupBy(hf => new { hf.District.Region.State.StateId, hf.District.Region.State.StateName })
                        .Select(g => new ChartDataDTO
                        {
                            Label = g.Key.StateName,
                            Value = g.Count()
                        })
                        .OrderByDescending(x => x.Value)
                        .ToListAsync();
                    break;

                case "region":
                    chartData = await query
                        .GroupBy(hf => new { hf.District.Region.RegionId, hf.District.Region.RegionName })
                        .Select(g => new ChartDataDTO
                        {
                            Label = g.Key.RegionName,
                            Value = g.Count()
                        })
                        .OrderByDescending(x => x.Value)
                        .ToListAsync();
                    break;

                case "facilitytype":
                    chartData = await query
                        .GroupBy(hf => new { hf.FacilityType.FacilityTypeId, hf.FacilityType.TypeName })
                        .Select(g => new ChartDataDTO
                        {
                            Label = g.Key.TypeName,
                            Value = g.Count()
                        })
                        .OrderByDescending(x => x.Value)
                        .ToListAsync();
                    break;

                default:
                    return BadRequest(ApiResponse<List<ChartDataDTO>>.ErrorResult("Invalid groupBy parameter. Must be 'state', 'region', or 'facilitytype'"));
            }

            return Ok(ApiResponse<List<ChartDataDTO>>.SuccessResult(chartData, "Bar chart data retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bar chart data");
            return StatusCode(500, ApiResponse<List<ChartDataDTO>>.ErrorResult("An error occurred while retrieving bar chart data", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Get pie chart data for facilities by state, region, or facility type
    /// </summary>
    /// <param name="groupBy">Group by: 'state', 'region', or 'facilitytype'</param>
    /// <param name="stateId">Optional filter by state ID</param>
    /// <param name="regionId">Optional filter by region ID</param>
    /// <param name="facilityTypeId">Optional filter by facility type ID</param>
    [HttpGet("piechart")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<ChartDataDTO>>>> GetPieChartData(
        [FromQuery] string groupBy = "state",
        [FromQuery] int? stateId = null,
        [FromQuery] int? regionId = null,
        [FromQuery] int? facilityTypeId = null)
    {
        try
        {
            var query = _context.HealthFacilities.AsQueryable();

            // Apply filters
            if (stateId.HasValue)
            {
                query = query.Where(hf => hf.District.Region.StateId == stateId.Value);
            }

            if (regionId.HasValue)
            {
                query = query.Where(hf => hf.District.RegionId == regionId.Value);
            }

            if (facilityTypeId.HasValue)
            {
                query = query.Where(hf => hf.FacilityTypeId == facilityTypeId.Value);
            }

            List<ChartDataDTO> chartData;

            switch (groupBy.ToLower())
            {
                case "state":
                    chartData = await query
                        .GroupBy(hf => new { hf.District.Region.State.StateId, hf.District.Region.State.StateName })
                        .Select(g => new ChartDataDTO
                        {
                            Label = g.Key.StateName,
                            Value = g.Count()
                        })
                        .OrderByDescending(x => x.Value)
                        .ToListAsync();
                    break;

                case "region":
                    chartData = await query
                        .GroupBy(hf => new { hf.District.Region.RegionId, hf.District.Region.RegionName })
                        .Select(g => new ChartDataDTO
                        {
                            Label = g.Key.RegionName,
                            Value = g.Count()
                        })
                        .OrderByDescending(x => x.Value)
                        .ToListAsync();
                    break;

                case "facilitytype":
                    chartData = await query
                        .GroupBy(hf => new { hf.FacilityType.FacilityTypeId, hf.FacilityType.TypeName })
                        .Select(g => new ChartDataDTO
                        {
                            Label = g.Key.TypeName,
                            Value = g.Count()
                        })
                        .OrderByDescending(x => x.Value)
                        .ToListAsync();
                    break;

                default:
                    return BadRequest(ApiResponse<List<ChartDataDTO>>.ErrorResult("Invalid groupBy parameter. Must be 'state', 'region', or 'facilitytype'"));
            }

            return Ok(ApiResponse<List<ChartDataDTO>>.SuccessResult(chartData, "Pie chart data retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pie chart data");
            return StatusCode(500, ApiResponse<List<ChartDataDTO>>.ErrorResult("An error occurred while retrieving pie chart data", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Get statistics for all states (regions, districts, facilities count)
    /// </summary>
    [HttpGet("state-statistics")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<StateStatisticsDTO>>>> GetAllStateStatistics()
    {
        try
        {
            var states = await _context.States
                .OrderBy(s => s.StateName)
                .ToListAsync();

            var stateStatisticsList = new List<StateStatisticsDTO>();

            foreach (var state in states)
            {
                var totalRegions = await _context.Regions
                    .Where(r => r.StateId == state.StateId)
                    .CountAsync();

                var totalDistricts = await _context.Districts
                    .Where(d => d.Region.StateId == state.StateId)
                    .CountAsync();

                var totalFacilities = await _context.HealthFacilities
                    .Where(hf => hf.District.Region.StateId == state.StateId)
                    .CountAsync();

                // Get region statistics for this state
                var regions = await _context.Regions
                    .Where(r => r.StateId == state.StateId)
                    .Select(r => new RegionStatisticsDTO
                    {
                        Name = r.RegionName,
                        TotalDistrict = _context.Districts.Count(d => d.RegionId == r.RegionId),
                        TotalFacility = _context.HealthFacilities.Count(hf => hf.District.RegionId == r.RegionId)
                    })
                    .ToListAsync();

                var statistics = new StateStatisticsDTO
                {
                    StateId = state.StateId,
                    StateName = state.StateName,
                    StateCode = state.StateCode,
                    Regions = regions,
                    TotalRegions = totalRegions,
                    TotalDistricts = totalDistricts,
                    TotalFacilities = totalFacilities
                };

                stateStatisticsList.Add(statistics);
            }

            return Ok(ApiResponse<List<StateStatisticsDTO>>.SuccessResult(stateStatisticsList, "State statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all state statistics");
            return StatusCode(500, ApiResponse<List<StateStatisticsDTO>>.ErrorResult("An error occurred while retrieving state statistics", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Get statistics for a specific state (regions, districts, facilities count)
    /// </summary>
    /// <param name="stateId">State ID</param>
    [HttpGet("state-statistics/{stateId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<StateStatisticsDTO>>> GetStateStatisticsById(int stateId)
    {
        return await GetStateStatistics(stateId);
    }

    /// <summary>
    /// Internal method to get statistics for a specific state or all states
    /// </summary>
    /// <param name="stateId">Optional State ID. If not provided, returns statistics for all states</param>
    private async Task<ActionResult<ApiResponse<StateStatisticsDTO>>> GetStateStatistics(int? stateId = null)
    {
        try
        {
            if (stateId.HasValue)
            {
                // Get statistics for a specific state
                var state = await _context.States
                    .FirstOrDefaultAsync(s => s.StateId == stateId.Value);

                if (state == null)
                {
                    return NotFound(ApiResponse<StateStatisticsDTO>.ErrorResult($"State with ID {stateId.Value} not found"));
                }

                var totalRegions = await _context.Regions
                    .Where(r => r.StateId == stateId.Value)
                    .CountAsync();

                var totalDistricts = await _context.Districts
                    .Where(d => d.Region.StateId == stateId.Value)
                    .CountAsync();

                var totalFacilities = await _context.HealthFacilities
                    .Where(hf => hf.District.Region.StateId == stateId.Value)
                    .CountAsync();

                // Get region statistics for this state
                var regions = await _context.Regions
                    .Where(r => r.StateId == stateId.Value)
                    .Select(r => new RegionStatisticsDTO
                    {
                        Name = r.RegionName,
                        TotalDistrict = _context.Districts.Count(d => d.RegionId == r.RegionId),
                        TotalFacility = _context.HealthFacilities.Count(hf => hf.District.RegionId == r.RegionId)
                    })
                    .ToListAsync();

                var statistics = new StateStatisticsDTO
                {
                    StateId = state.StateId,
                    StateName = state.StateName,
                    StateCode = state.StateCode,
                    Regions = regions,
                    TotalRegions = totalRegions,
                    TotalDistricts = totalDistricts,
                    TotalFacilities = totalFacilities
                };

                return Ok(ApiResponse<StateStatisticsDTO>.SuccessResult(statistics, "State statistics retrieved successfully"));
            }
            else
            {
                // Get statistics for all states (aggregated)
                var totalStates = await _context.States.CountAsync();
                var totalRegions = await _context.Regions.CountAsync();
                var totalDistricts = await _context.Districts.CountAsync();
                var totalFacilities = await _context.HealthFacilities.CountAsync();

                // Get region statistics for all states
                var regions = await _context.Regions
                    .Select(r => new RegionStatisticsDTO
                    {
                        Name = r.RegionName,
                        TotalDistrict = _context.Districts.Count(d => d.RegionId == r.RegionId),
                        TotalFacility = _context.HealthFacilities.Count(hf => hf.District.RegionId == r.RegionId)
                    })
                    .ToListAsync();

                var statistics = new StateStatisticsDTO
                {
                    StateId = 0,
                    StateName = "All States",
                    StateCode = "ALL",
                    Regions = regions,
                    TotalRegions = totalRegions,
                    TotalDistricts = totalDistricts,
                    TotalFacilities = totalFacilities
                };

                return Ok(ApiResponse<StateStatisticsDTO>.SuccessResult(statistics, "State statistics retrieved successfully"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving state statistics for state ID {StateId}", stateId);
            return StatusCode(500, ApiResponse<StateStatisticsDTO>.ErrorResult("An error occurred while retrieving state statistics", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Get top 3 regions with highest number of facilities
    /// </summary>
    [HttpGet("top-regions")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<TopRegionDTO>>>> GetTopRegions()
    {
        try
        {
            var topRegions = await _context.HealthFacilities
                .GroupBy(hf => new
                {
                    hf.District.Region.RegionId,
                    hf.District.Region.RegionName,
                    hf.District.Region.StateId,
                    hf.District.Region.State.StateName
                })
                .Select(g => new TopRegionDTO
                {
                    RegionId = g.Key.RegionId,
                    RegionName = g.Key.RegionName,
                    StateId = g.Key.StateId,
                    StateName = g.Key.StateName,
                    FacilityCount = g.Count()
                })
                .OrderByDescending(x => x.FacilityCount)
                .Take(3)
                .ToListAsync();

            return Ok(ApiResponse<List<TopRegionDTO>>.SuccessResult(topRegions, "Top regions retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top regions");
            return StatusCode(500, ApiResponse<List<TopRegionDTO>>.ErrorResult("An error occurred while retrieving top regions", new List<string> { ex.Message }));
        }
    }
}
