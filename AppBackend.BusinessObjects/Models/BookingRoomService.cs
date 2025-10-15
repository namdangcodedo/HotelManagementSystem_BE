using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace AppBackend.BusinessObjects.Models
{
    [Table("BookingRoomService")]
    public class BookingRoomService
    {
        [Key]
        public int BookingRoomServiceId { get; set; }

        [Required]
        [ForeignKey("BookingRoom")]
        public int BookingRoomId { get; set; }
        public virtual BookingRoom BookingRoom { get; set; } = null!;

        [Required]
        [ForeignKey("Service")]
        public int ServiceId { get; set; }
        public virtual Service Service { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAtTime { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;
    }
}

