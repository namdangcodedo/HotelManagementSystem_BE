using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("HousekeepingTask")]
public class HousekeepingTask
{
    [Key]
    public int TaskId { get; set; }

    [Required]
    [ForeignKey("Room")]
    public int RoomId { get; set; }

    [ForeignKey("Janitor")]
    public int? JanitorId { get; set; }

    [Required]
    [ForeignKey("TaskType")]
    public int TaskTypeId { get; set; }
    public virtual CommonCode TaskType { get; set; } = null!;

    [Required]
    [ForeignKey("Status")]
    public int StatusId { get; set; }
    public virtual CommonCode Status { get; set; } = null!;

    [Required]
    public DateTime DueDate { get; set; }

    public DateTime? CompletedAt { get; set; }

    [StringLength(255)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual Employee? Janitor { get; set; }
    public virtual Room Room { get; set; } = null!;
}
