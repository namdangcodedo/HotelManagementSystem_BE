using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

/// <summary>
/// Room - Phòng cụ thể trong khách sạn
/// Ví dụ: Phòng 101, 102, 201...
/// </summary>
[Table("Room")]
public class Room
{
  [Key]
  public int RoomId { get; set; }

  [Required]
  [StringLength(100)]
  public string RoomName { get; set; } = null!;

  [Required]
  [ForeignKey("RoomType")]
  public int RoomTypeId { get; set; }
  public virtual RoomType RoomType { get; set; } = null!;

  [Required]
  [ForeignKey("Status")]
  public int StatusId { get; set; }
  public virtual CommonCode Status { get; set; } = null!;

  [StringLength(500)]
  public string? Description { get; set; }

  [Required]
  public DateTime CreatedAt { get; set; }

  public int? CreatedBy { get; set; }

  public DateTime? UpdatedAt { get; set; }

  public int? UpdatedBy { get; set; }

  // Navigation properties
  public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
  public virtual ICollection<HousekeepingTask> HousekeepingTasks { get; set; } = new List<HousekeepingTask>();
  public virtual ICollection<Medium> Media { get; set; } = new List<Medium>();
  public virtual ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();
  public virtual ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();
}
