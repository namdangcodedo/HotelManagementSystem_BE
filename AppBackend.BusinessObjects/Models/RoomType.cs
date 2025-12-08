using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

/// <summary>
/// RoomType - Loại phòng với thông tin giá cả và cấu hình
/// Ví dụ: Standard Room, Deluxe Room, Suite...
/// </summary>
[Table("RoomType")]
public class RoomType
{
  [Key]
  public int RoomTypeId { get; set; }

  [Required]
  [StringLength(100)]
  public string TypeName { get; set; } = null!;

  [Required]
  [StringLength(50)]
  public string TypeCode { get; set; } = null!;

  [StringLength(500)]
  public string? Description { get; set; }

  /// <summary>
  /// Giá cơ bản theo đêm
  /// </summary>
  [Required]
  [Column(TypeName = "decimal(18,2)")]
  public decimal BasePriceNight { get; set; }

  /// <summary>
  /// Số lượng người tối đa
  /// </summary>
  [Required]
  public int MaxOccupancy { get; set; }

  /// <summary>
  /// Diện tích phòng (m2)
  /// </summary>
  [Column(TypeName = "decimal(10,2)")]
  public decimal? RoomSize { get; set; }

  /// <summary>
  /// Số giường
  /// </summary>
  public int? NumberOfBeds { get; set; }

  /// <summary>
  /// Loại giường (King, Queen, Twin...)
  /// </summary>
  [StringLength(50)]
  public string? BedType { get; set; }

  [Required]
  public bool IsActive { get; set; }

  [Required]
  public DateTime CreatedAt { get; set; }

  public int? CreatedBy { get; set; }

  public DateTime? UpdatedAt { get; set; }

  public int? UpdatedBy { get; set; }

  // Navigation properties
  public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
  public virtual ICollection<Medium> Media { get; set; } = new List<Medium>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

}
