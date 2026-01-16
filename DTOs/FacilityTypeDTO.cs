namespace SMHFR_BE.DTOs;

public class FacilityTypeDTO
{
    public int FacilityTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
