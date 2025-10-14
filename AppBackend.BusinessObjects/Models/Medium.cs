using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("Medium")]
public partial class Medium
{
    [Key]
    public int MediaId { get; set; }

    [ForeignKey("Room")]
    public int? RoomId { get; set; }

    [StringLength(100)]
    public string? PublishId { get; set; }

    [ForeignKey("Service")]
    public int? ServiceId { get; set; }

    [ForeignKey("Customer")]
    public int? CustomerId { get; set; }

    [Required]
    [StringLength(255)]
    public string FilePath { get; set; } = null!;

    [StringLength(255)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual Room? Room { get; set; }

    public virtual Service? Service { get; set; }

    public virtual Customer? Customer { get; set; }
}
