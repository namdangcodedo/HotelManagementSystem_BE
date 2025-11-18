namespace AppBackend.Services.ApiModels.BookingModel
{
    /// <summary>
    /// Request phân trang và filter danh sách booking
    /// </summary>
    public class GetBookingListRequest
    {
        // Phân trang
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Filter theo ngày
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Filter theo trạng thái (lấy từ CommonCode)
        public string? BookingStatus { get; set; } // Pending, Confirmed, CheckedIn, CheckedOut, Cancelled
        public string? PaymentStatus { get; set; } // Paid, Unpaid, PartiallyPaid, Refunded
        public string? DepositStatus { get; set; } // Paid, Unpaid

        // Filter theo loại booking
        public string? BookingType { get; set; } // Online, Walkin

        // Tìm kiếm
        public string? CustomerName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? BookingCode { get; set; } // Mã booking

        // Sắp xếp
        public string? SortBy { get; set; } // CreatedAt, CheckInDate, TotalAmount
        public bool IsDescending { get; set; } = true;
    }

    /// <summary>
    /// Response danh sách booking với phân trang
    /// </summary>
    public class PagedBookingListResponse
    {
        public List<BookingListItemDto> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }

    /// <summary>
    /// DTO cho từng item trong danh sách booking
    /// </summary>
    public class BookingListItemDto
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        
        // Thông tin khách hàng
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;

        // Thông tin booking
        public string BookingType { get; set; } = string.Empty; // Online, Walkin
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int TotalNights { get; set; }
        public int TotalRooms { get; set; }

        // Thông tin phòng tóm tắt
        public List<string> RoomNumbers { get; set; } = new();
        public List<string> RoomTypes { get; set; } = new();

        // Thông tin tài chính
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }

        // Trạng thái
        public string BookingStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string DepositStatus { get; set; } = string.Empty;

        // Thông tin tạo
        public DateTime CreatedAt { get; set; }
        public string? CreatedByEmployee { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO chi tiết booking đầy đủ
    /// </summary>
    public class BookingDetailDto
    {
        // Thông tin cơ bản
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string BookingType { get; set; } = string.Empty;

        // Thông tin khách hàng
        public CustomerDetailDto Customer { get; set; } = new();

        // Thông tin phòng
        public List<BookingRoomDetailDto> Rooms { get; set; } = new();

        // Thông tin thời gian
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int TotalNights { get; set; }

        // Thông tin tài chính
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ServiceCharge { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }

        // Trạng thái
        public string BookingStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string DepositStatus { get; set; } = string.Empty;

        // Yêu cầu đặc biệt
        public string? SpecialRequests { get; set; }

        // Lịch sử thanh toán
        public List<PaymentHistoryDetailDto> PaymentHistory { get; set; } = new();

        // Lịch sử thay đổi booking
        public List<BookingHistoryDto> BookingHistory { get; set; } = new();

        // Thông tin tạo/cập nhật
        public DateTime CreatedAt { get; set; }
        public string? CreatedByEmployee { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedByEmployee { get; set; }

        // Thông tin hủy (nếu có)
        public DateTime? CancelledAt { get; set; }
        public string? CancelledBy { get; set; }
        public string? CancellationReason { get; set; }
    }

    /// <summary>
    /// DTO thông tin khách hàng chi tiết
    /// </summary>
    public class CustomerDetailDto
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? IdentityCard { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        
        // Thống kê
        public int TotalBookings { get; set; }
        public DateTime? LastBookingDate { get; set; }
        public decimal TotalSpent { get; set; }
    }

    /// <summary>
    /// DTO thông tin phòng trong booking
    /// </summary>
    public class BookingRoomDetailDto
    {
        public int BookingRoomId { get; set; }
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomTypeName { get; set; } = string.Empty;
        public string RoomTypeCode { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public int NumberOfNights { get; set; }
        public decimal SubTotal { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> RoomImages { get; set; } = new();
        
        // Thông tin chi tiết phòng
        public int MaxOccupancy { get; set; }
        public decimal RoomSize { get; set; }
        public string BedType { get; set; } = string.Empty;
        public List<string> Amenities { get; set; } = new();
    }

    /// <summary>
    /// DTO lịch sử thanh toán chi tiết
    /// </summary>
    public class PaymentHistoryDetailDto
    {
        public int TransactionId { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, Transfer, PayOS
        public string TransactionType { get; set; } = string.Empty; // Deposit, FullPayment, PartialPayment, Refund
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string? TransactionReference { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string ProcessedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO lịch sử thay đổi booking
    /// </summary>
    public class BookingHistoryDto
    {
        public int HistoryId { get; set; }
        public string Action { get; set; } = string.Empty; // Created, Updated, Confirmed, CheckedIn, CheckedOut, Cancelled
        public string? Description { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request cập nhật trạng thái booking
    /// </summary>
    public class UpdateBookingStatusRequest
    {
        public string Status { get; set; } = string.Empty; // Confirmed, CheckedIn, CheckedOut, Cancelled
        public string? Note { get; set; }
    }

    /// <summary>
    /// Request thống kê booking
    /// </summary>
    public class BookingStatisticsRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? GroupBy { get; set; } // Day, Week, Month, Year
    }

    /// <summary>
    /// Response thống kê booking
    /// </summary>
    public class BookingStatisticsResponse
    {
        public int TotalBookings { get; set; }
        public int TotalOnlineBookings { get; set; }
        public int TotalWalkinBookings { get; set; }
        public int TotalConfirmedBookings { get; set; }
        public int TotalCancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal AverageBookingValue { get; set; }
        public List<BookingStatisticsItemDto> StatisticsByPeriod { get; set; } = new();
    }

    /// <summary>
    /// DTO thống kê theo thời gian
    /// </summary>
    public class BookingStatisticsItemDto
    {
        public DateTime Date { get; set; }
        public string Period { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageBookingValue { get; set; }
    }

    /// <summary>
    /// Request export booking
    /// </summary>
    public class ExportBookingRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? BookingStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? BookingType { get; set; }
        public string ExportFormat { get; set; } = "Excel"; // Excel, PDF, CSV
    }
}

