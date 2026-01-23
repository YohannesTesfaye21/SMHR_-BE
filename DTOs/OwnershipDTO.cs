namespace SMHFR_BE.DTOs;

public class OwnershipDTO
{
    public int OwnershipId { get; set; }
    public string OwnershipType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
