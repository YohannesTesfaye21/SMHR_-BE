using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMHFR_BE.Models;

[Table("FacilityTypes")]
public class FacilityType
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int FacilityTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string TypeName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<HealthFacility> HealthFacilities { get; set; } = new List<HealthFacility>();
}
