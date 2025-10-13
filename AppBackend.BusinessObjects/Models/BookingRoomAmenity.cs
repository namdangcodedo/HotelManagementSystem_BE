using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace AppBackend.BusinessObjects.Models
{
    [Table("BookingRoomAmenity")]
    public class BookingRoomAmenity
    {
        [Key]
        public int BookingRoomAmenityId { get; set; }

        [Required]
        [ForeignKey("BookingRoom")]
        public int BookingRoomId { get; set; }
        public virtual BookingRoom BookingRoom { get; set; } = null!;

        [Required]
        [ForeignKey("Amenity")]
        public int AmenityId { get; set; }
        public virtual Amenity Amenity { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAtTime { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;
    }
}
