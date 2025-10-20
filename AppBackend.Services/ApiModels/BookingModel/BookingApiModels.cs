namespace AppBackend.Services.ApiModels.BookingModel
{
    public class CreateBookingRequest
    {
        public int CustomerId { get; set; }
        public List<int> RoomIds { get; set; } = new List<int>();
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
        public List<int> RoomIds { get; set; } = new List<int>();
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string? SpecialRequests { get; set; }
        public string BookingType { get; set; } = "Online"; // Online, Walkin
    }

    public class CheckRoomAvailabilityRequest
    {
        public List<int> RoomIds { get; set; } = new List<int>();
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
    }

    public class BookingDto
    {
        public int BookingId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public List<int> RoomIds { get; set; } = new List<int>();
        public List<string> RoomNumbers { get; set; } = new List<string>();
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string DepositStatus { get; set; } = string.Empty;
        public string BookingType { get; set; } = string.Empty;
        public string? SpecialRequests { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? PaymentUrl { get; set; } // PayOS payment URL
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
        public string RoomNumber { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string LockedBy { get; set; } = string.Empty;
        public DateTime LockExpiry { get; set; }
    }
}
