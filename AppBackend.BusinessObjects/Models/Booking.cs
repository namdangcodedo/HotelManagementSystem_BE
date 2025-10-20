using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("Booking")]
public class Booking
{
  [Key]
  public int BookingId { get; set; }

  [Required]
  [ForeignKey("Customer")]
  public int CustomerId { get; set; }
  public virtual Customer Customer { get; set; } = null!;

  [Required]
  public DateTime CheckInDate { get; set; }

  [Required]
  public DateTime CheckOutDate { get; set; }

  [Required]
  [Column(TypeName = "decimal(18,2)")]
  public decimal TotalAmount { get; set; }

  [Column(TypeName = "decimal(18,2)")]
  public decimal DepositAmount { get; set; }

  [ForeignKey("PaymentStatus")]
  public int? PaymentStatusId { get; set; }
  public virtual CommonCode? PaymentStatus { get; set; }

  [ForeignKey("DepositStatus")]
  public int? DepositStatusId { get; set; }
  public virtual CommonCode? DepositStatus { get; set; }

  [ForeignKey("BookingType")]
  public int? BookingTypeId { get; set; }
  public virtual CommonCode? BookingType { get; set; }

  [StringLength(500)]
  public string? SpecialRequests { get; set; }

  [Required]
  public DateTime CreatedAt { get; set; }

  public int? CreatedBy { get; set; }

  public DateTime? UpdatedAt { get; set; }

  public int? UpdatedBy { get; set; }

  // Navigation properties
  public virtual ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();
  public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
  public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
  public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
}
