using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("BookingDtos")]
public class Booking
{
    [Key]
    public int BookingId { get; set; }

    [Required]
    [ForeignKey("Customer")]
    public int CustomerId { get; set; }

    [Required]
    [ForeignKey("Room")]
    public int RoomId { get; set; }

    [Required]
    [ForeignKey("BookingType")]
    public int BookingTypeId { get; set; }
    public virtual CommonCode BookingType { get; set; } = null!;

    [Required]
    public DateTime CheckIn { get; set; }

    [Required]
    public DateTime CheckOut { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedPrice { get; set; }

    [Required]
    [ForeignKey("Status")]
    public int StatusId { get; set; }
    public virtual CommonCode Status { get; set; } = null!;

    [StringLength(255)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual Room Room { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();
}
