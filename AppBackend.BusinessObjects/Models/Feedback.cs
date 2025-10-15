using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("Feedback")]
public class Feedback
{
    [Key]
    public int FeedbackId { get; set; }

    [ForeignKey("Customer")]
    public int? CustomerId { get; set; }

    [ForeignKey("Booking")]
    public int? BookingId { get; set; }

    [StringLength(100)]
    public string? Subject { get; set; }

    [Required]
    [StringLength(1000)]
    public string Content { get; set; } = null!;

    public int? Rating { get; set; }

    [Required]
    [ForeignKey("FeedbackType")]
    public int FeedbackTypeId { get; set; }
    public virtual CommonCode FeedbackType { get; set; } = null!;

    [Required]
    [ForeignKey("Status")]
    public int StatusId { get; set; }
    public virtual CommonCode Status { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual Booking? Booking { get; set; }
    public virtual Customer? Customer { get; set; }
}
