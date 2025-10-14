using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

public class HolidayPricing
{
  [Key]
  public int HolidayPricingId { get; set; }
  [Required]
  [ForeignKey("Holiday")]
  public int HolidayId { get; set; }
  [ForeignKey("Room")]
  public int? RoomId { get; set; }
  [ForeignKey("Service")]
  public int? ServiceId { get; set; }
  [Required]
  [Column(TypeName = "decimal(18,2)")]
  public decimal PriceAdjustment { get; set; }
  [Required]
  public DateTime StartDate { get; set; }
  [Required]
  public DateTime EndDate { get; set; }
  [Required]
  public bool IsActive { get; set; }
  public DateTime? ExpiredDate { get; set; }
  public virtual Holiday Holiday { get; set; } = null!;
  public virtual Room? Room { get; set; }
  public virtual Service? Service { get; set; }
}
