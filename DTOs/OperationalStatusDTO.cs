namespace SMHFR_BE.DTOs;

public class OperationalStatusDTO
{
    public int OperationalStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
