using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMHFR_BE.Models;

[Table("States")]
public class State
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int StateId { get; set; }

    [Required]
    [MaxLength(255)]
    public string StateCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string StateName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Region> Regions { get; set; } = new List<Region>();
}
