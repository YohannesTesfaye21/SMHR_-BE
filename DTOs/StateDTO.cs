namespace SMHFR_BE.DTOs;

public class StateDTO
{
    public int StateId { get; set; }
    public string StateCode { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
