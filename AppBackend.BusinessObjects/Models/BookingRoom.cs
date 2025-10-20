using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models
{
    /// <summary>
    /// BookingRoom - Chi tiết đơn đặt phòng (như OrderDetail)
    /// Lưu thông tin: phòng nào, giá bao nhiêu tại thời điểm đặt, checkin/checkout
    /// </summary>
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

        /// <summary>
        /// Giá phòng tại thời điểm đặt (có thể khác giá hiện tại)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerNight { get; set; }

        /// <summary>
        /// Số đêm
        /// </summary>
        [Required]
        public int NumberOfNights { get; set; }

        /// <summary>
        /// Tổng tiền = PricePerNight * NumberOfNights
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        // Navigation properties
        public virtual ICollection<BookingRoomService> BookingRoomServices { get; set; } = new List<BookingRoomService>();
    }
}
