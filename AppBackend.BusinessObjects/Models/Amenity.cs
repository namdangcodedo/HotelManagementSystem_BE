using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AppBackend.BusinessObjects.Enums;

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
    [StringLength(50)]
    public string AmenityType { get; set; } = nameof(Enums.AmenityType.Common); 

    [Required]
    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();
}
