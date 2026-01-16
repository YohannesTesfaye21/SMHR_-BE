namespace SMHFR_BE.DTOs;

public class RegionDTO
{
    public int RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;
    public StateDTO State { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
