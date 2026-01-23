using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMHFR_BE.Models;

[Table("OperationalStatuses")]
public class OperationalStatus
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int OperationalStatusId { get; set; }

    [Required]
    [MaxLength(50)]
    public string StatusName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<HealthFacility> HealthFacilities { get; set; } = new List<HealthFacility>();
}
