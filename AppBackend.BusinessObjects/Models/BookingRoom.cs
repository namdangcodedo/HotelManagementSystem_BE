using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models
{
    [Table("BookingRoom")]
    public class BookingRoom
    {
        [Key]
        public int BookingRoomId { get; set; }

        [Required]
        [ForeignKey("Booking")]
        public int BookingId { get; set; }
        public virtual Booking Booking { get; set; } = null!;

        [Required]
        [ForeignKey("Room")]
        public int RoomId { get; set; }
        public virtual Room Room { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAtTime { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;

        [Required]
        [ForeignKey("Account")]
        public int BookedByAccountId { get; set; }
        public virtual Account BookedByAccount { get; set; } = null!;

        public virtual ICollection<BookingRoomAmenity> BookingRoomAmenities { get; set; } = new List<BookingRoomAmenity>();
        public virtual ICollection<BookingRoomService> BookingRoomServices { get; set; } = new List<BookingRoomService>();
    }
}
