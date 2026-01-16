namespace SMHFR_BE.DTOs;

public class DistrictDTO
{
    public int DistrictId { get; set; }
    public string DistrictName { get; set; } = string.Empty;
    public RegionDTO Region { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
