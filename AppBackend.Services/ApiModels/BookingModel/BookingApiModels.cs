namespace AppBackend.Services.ApiModels.BookingModel
{
    /// <summary>
    /// Model để chỉ định loại phòng và số lượng cần đặt
    /// </summary>
    public class RoomTypeQuantityRequest
    {
        /// <summary>
        /// ID loại phòng (1=Standard, 2=Deluxe, 3=VIP, 4=Suite)
        /// </summary>
        public int RoomTypeId { get; set; }
        
        /// <summary>
        /// Số lượng phòng cần đặt
        /// </summary>
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Request để đặt phòng - chọn theo loại phòng, hệ thống sẽ tự động chọn phòng available
    /// </summary>
    public class CreateBookingRequest
    {
        public int CustomerId { get; set; }
        
        /// <summary>
        /// Danh sách loại phòng và số lượng cần đặt
        /// Ví dụ: [{ "roomTypeId": 1, "quantity": 2 }, { "roomTypeId": 3, "quantity": 1 }]
        /// </summary>
        public List<RoomTypeQuantityRequest> RoomTypes { get; set; } = new List<RoomTypeQuantityRequest>();
        
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string? SpecialRequests { get; set; }
    }

    /// <summary>
    /// Guest Booking - không cần tài khoản, chỉ cần thông tin customer
    /// </summary>
    public class CreateGuestBookingRequest
    {
        // Customer Information
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? IdentityCard { get; set; }
        public string? Address { get; set; }

        // Booking Information
        /// <summary>
        /// Danh sách loại phòng và số lượng cần đặt
        /// Ví dụ: [{ "roomTypeId": 1, "quantity": 2 }, { "roomTypeId": 3, "quantity": 1 }]
        /// </summary>
        public List<RoomTypeQuantityRequest> RoomTypes { get; set; } = new List<RoomTypeQuantityRequest>();
        
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string? SpecialRequests { get; set; }
    }

    /// <summary>
    /// Request kiểm tra phòng available theo loại phòng
    /// </summary>
    public class CheckRoomAvailabilityRequest
    {
        /// <summary>
        /// Danh sách loại phòng và số lượng cần kiểm tra
        /// Ví dụ: [{ "roomTypeId": 1, "quantity": 2 }, { "roomTypeId": 3, "quantity": 1 }]
        /// </summary>
        public List<RoomTypeQuantityRequest> RoomTypes { get; set; } = new List<RoomTypeQuantityRequest>();
        
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
    }

    public class BookingDto
    {
        public int BookingId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public List<int> RoomIds { get; set; } = new List<int>();
        public List<string> RoomNames { get; set; } = new List<string>();
        public List<RoomTypeQuantityDto> RoomTypeDetails { get; set; } = new List<RoomTypeQuantityDto>();
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string DepositStatus { get; set; } = string.Empty;
        public string BookingType { get; set; } = string.Empty;
        public string? SpecialRequests { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? PaymentUrl { get; set; } 
    }

    public class RoomTypeQuantityDto
    {
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public string RoomTypeCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal PricePerNight { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class ConfirmPaymentRequest
    {
        public int BookingId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class RoomLockInfo
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string LockedBy { get; set; } = string.Empty;
        public DateTime LockExpiry { get; set; }
    }

    /// <summary>
    /// Thông tin phòng available theo loại - Response chi tiết
    /// </summary>
    public class RoomTypeAvailabilityDto
    {
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public string RoomTypeCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BasePriceNight { get; set; }
        public int MaxOccupancy { get; set; }
        public decimal RoomSize { get; set; }
        public int NumberOfBeds { get; set; }
        public string BedType { get; set; } = string.Empty;
        
        /// <summary>
        /// Số lượng phòng trống hiện có
        /// </summary>
        public int AvailableCount { get; set; }
        
        /// <summary>
        /// Số lượng phòng khách yêu cầu
        /// </summary>
        public int RequestedQuantity { get; set; }
        
        /// <summary>
        /// Có đủ phòng để đáp ứng yêu cầu không
        /// </summary>
        public bool IsAvailable { get; set; }
        
        /// <summary>
        /// Thông báo cho khách hàng
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Danh sách hình ảnh phòng
        /// </summary>
        public List<string> Images { get; set; } = new List<string>();
    }

    /// <summary>
    /// Response tổng hợp cho check availability
    /// </summary>
    public class CheckAvailabilityResponse
    {
        public bool IsAllAvailable { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<RoomTypeAvailabilityDto> RoomTypes { get; set; } = new List<RoomTypeAvailabilityDto>();
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int TotalNights { get; set; }
    }
}
