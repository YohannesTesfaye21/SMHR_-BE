using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMHFR_BE.Models;

[Table("Regions")]
public class Region
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RegionId { get; set; }

    [Required]
    public int StateId { get; set; }

    [Required]
    [MaxLength(100)]
    public string RegionName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("StateId")]
    public virtual State State { get; set; } = null!;

    public virtual ICollection<District> Districts { get; set; } = new List<District>();
}
