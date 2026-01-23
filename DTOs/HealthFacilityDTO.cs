namespace SMHFR_BE.DTOs;

public class HealthFacilityDTO
{
    public int HealthFacilityId { get; set; }
    public string FacilityId { get; set; } = string.Empty;
    public string HealthFacilityName { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    
    // Lookup DTOs (without circular references)
    public DistrictDTO District { get; set; } = null!;
    public FacilityTypeDTO FacilityType { get; set; } = null!;
    public OwnershipDTO Ownership { get; set; } = null!;
    public OperationalStatusDTO OperationalStatus { get; set; } = null!;
    
    // Partner and Project Information
    public string? HCPartners { get; set; }
    public DateTime? HCProjectEndDate { get; set; }
    public string? NutritionClusterPartners { get; set; }
    public string? DamalCaafimaadPartner { get; set; }
    public DateTime? DamalCaafimaadProjectEndDate { get; set; }
    public string? BetterLifeProjectPartner { get; set; }
    public DateTime? BetterLifeProjectEndDate { get; set; }
    public string? CaafimaadPlusPartner { get; set; }
    public DateTime? CaafimaadPlusProjectEndDate { get; set; }
    
    // Facility In-Charge Information
    public string? FacilityInChargeName { get; set; }
    public string? FacilityInChargeNumber { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
