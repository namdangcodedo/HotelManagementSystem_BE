using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("Amenity")]
public partial class Amenity
{
    [Key]
    public int AmenityId { get; set; }

    [Required]
    [StringLength(100)]
    public string AmenityName { get; set; } = null!;

    [StringLength(255)]
    public string? Description { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();

    public virtual ICollection<BookingRoomAmenity> BookingRoomAmenities { get; set; } = new List<BookingRoomAmenity>();
}
