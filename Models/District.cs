using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMHFR_BE.Models;

[Table("Districts")]
public class District
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int DistrictId { get; set; }

    [Required]
    public int RegionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string DistrictName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("RegionId")]
    public virtual Region Region { get; set; } = null!;

    public virtual ICollection<HealthFacility> HealthFacilities { get; set; } = new List<HealthFacility>();
}
