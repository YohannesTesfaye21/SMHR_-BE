using System.ComponentModel.DataAnnotations;

namespace SMHFR_BE.DTOs;

public class UpdateHealthFacilityDTO
{
    [Required]
    [MaxLength(50)]
    public string FacilityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string HealthFacilityName { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [Required]
    public int DistrictId { get; set; }

    [Required]
    public int FacilityTypeId { get; set; }

    [Required]
    public int OwnershipId { get; set; }

    [Required]
    public int OperationalStatusId { get; set; }

    // Partner and Project Information
    public string? HCPartners { get; set; }

    public DateTime? HCProjectEndDate { get; set; }

    [MaxLength(255)]
    public string? NutritionClusterPartners { get; set; }

    [MaxLength(255)]
    public string? DamalCaafimaadPartner { get; set; }

    public DateTime? DamalCaafimaadProjectEndDate { get; set; }

    [MaxLength(255)]
    public string? BetterLifeProjectPartner { get; set; }

    public DateTime? BetterLifeProjectEndDate { get; set; }

    [MaxLength(255)]
    public string? CaafimaadPlusPartner { get; set; }

    public DateTime? CaafimaadPlusProjectEndDate { get; set; }

    // Facility In-Charge Information
    [MaxLength(255)]
    public string? FacilityInChargeName { get; set; }

    [MaxLength(50)]
    public string? FacilityInChargeNumber { get; set; }
}
