using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMHFR_BE.Models;

[Table("HealthFacilities")]
public class HealthFacility
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int HealthFacilityId { get; set; }

    [Required]
    [MaxLength(50)]
    public string FacilityId { get; set; } = string.Empty; // New Facility ID from CSV

    [Required]
    [MaxLength(255)]
    public string HealthFacilityName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,7)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal? Longitude { get; set; }

    // Foreign Keys
    [Required]
    public int DistrictId { get; set; }

    [Required]
    public int FacilityTypeId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Ownership { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string OperationalStatus { get; set; } = string.Empty;

    // Partner and Project Information
    [Column(TypeName = "text")]
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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("DistrictId")]
    public virtual District District { get; set; } = null!;

    [ForeignKey("FacilityTypeId")]
    public virtual FacilityType FacilityType { get; set; } = null!;
}
