using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("Room")]
public class Room
{
  [Key]
  public int RoomId { get; set; }

  [Required]
  [StringLength(20)]
  public string RoomNumber { get; set; } = null!;

  [Required]
  [ForeignKey("RoomType")]
  public int RoomTypeId { get; set; }
  public virtual CommonCode RoomType { get; set; } = null!;

  [Required]
  [Column(TypeName = "decimal(18,2)")]
  public decimal BasePriceNight { get; set; }

  [Required]
  [Column(TypeName = "decimal(18,2)")]
  public decimal BasePriceHour { get; set; }

  [Required]
  [ForeignKey("Status")]
  public int StatusId { get; set; }
  public virtual CommonCode Status { get; set; } = null!;

  [StringLength(255)]
  public string? Description { get; set; }

  [Required]
  public DateTime CreatedAt { get; set; }

  public int? CreatedBy { get; set; }

  public DateTime? UpdatedAt { get; set; }

  public int? UpdatedBy { get; set; }

  public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
  public virtual ICollection<HousekeepingTask> HousekeepingTasks { get; set; } = new List<HousekeepingTask>();
  public virtual ICollection<Medium> Media { get; set; } = new List<Medium>();
  public virtual ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();
}
