namespace SMHFR_BE.DTOs;

/// <summary>
/// DTO for dashboard card statistics
/// </summary>
public class CardDataDTO
{
    public int TotalStates { get; set; }
    public int TotalRegions { get; set; }
    public int TotalDistricts { get; set; }
    public int TotalFacilities { get; set; }
}

/// <summary>
/// DTO for chart data (bar and pie charts)
/// </summary>
public class ChartDataDTO
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

/// <summary>
/// DTO for region statistics within a state
/// </summary>
public class RegionStatisticsDTO
{
    public string Name { get; set; } = string.Empty;
    public int TotalFacility { get; set; }
    public int TotalDistrict { get; set; }
}

/// <summary>
/// DTO for state statistics
/// </summary>
public class StateStatisticsDTO
{
    public int StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public List<RegionStatisticsDTO> Regions { get; set; } = new List<RegionStatisticsDTO>();
    public int TotalRegions { get; set; }
    public int TotalDistricts { get; set; }
    public int TotalFacilities { get; set; }
}

/// <summary>
/// DTO for top regions by facility count
/// </summary>
public class TopRegionDTO
{
    public int RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;
    public int StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public int FacilityCount { get; set; }
}
