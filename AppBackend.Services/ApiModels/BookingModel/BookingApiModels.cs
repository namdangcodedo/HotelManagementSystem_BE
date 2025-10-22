namespace AppBackend.Services.ApiModels.BookingModel
{
    /// <summary>
    /// Request để đặt phòng - chọn theo loại phòng, hệ thống sẽ tự động chọn phòng available
    /// </summary>
    public class CreateBookingRequest
    {
        public int CustomerId { get; set; }
        
        /// <summary>
        /// Danh sách loại phòng và số lượng cần đặt
        /// Key: RoomTypeId, Value: Số lượng phòng
        /// Ví dụ: { {1, 2}, {3, 1} } = 2 phòng Standard + 1 phòng VIP
        /// </summary>
        public Dictionary<int, int> RoomTypeQuantities { get; set; } = new Dictionary<int, int>();
        
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string? SpecialRequests { get; set; }
        public string BookingType { get; set; } = "Online"; // Online, Walkin
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
        /// Key: RoomTypeId, Value: Số lượng phòng
        /// </summary>
        public Dictionary<int, int> RoomTypeQuantities { get; set; } = new Dictionary<int, int>();
        
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string? SpecialRequests { get; set; }
        public string BookingType { get; set; } = "Online"; // Online, Walkin
    }

    /// <summary>
    /// Request kiểm tra phòng available theo loại phòng
    /// </summary>
    public class CheckRoomAvailabilityRequest
    {
        /// <summary>
        /// Danh sách loại phòng và số lượng cần kiểm tra
        /// Key: RoomTypeId, Value: Số lượng phòng
        /// </summary>
        public Dictionary<int, int> RoomTypeQuantities { get; set; } = new Dictionary<int, int>();
        
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
        public int Quantity { get; set; }
        public decimal PricePerNight { get; set; }
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
    /// Thông tin phòng available theo loại
    /// </summary>
    public class RoomTypeAvailabilityDto
    {
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public string RoomTypeCode { get; set; } = string.Empty;
        public decimal BasePriceNight { get; set; }
        public int MaxOccupancy { get; set; }
        public int AvailableCount { get; set; }
        public int RequestedQuantity { get; set; }
        public bool IsAvailable { get; set; }
    }
}
