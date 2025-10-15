using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("RoomAmenity")]
public partial class RoomAmenity
{
    [Key]
    [Column(Order = 0)]
    [ForeignKey("Room")]
    public int RoomId { get; set; }
    [Key]
    [Column(Order = 1)]
    [ForeignKey("AmenityServices")]
    public int AmenityId { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public virtual Amenity Amenity { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;
}
