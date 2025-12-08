using System.ComponentModel.DataAnnotations;
using AppBackend.BusinessObjects.Dtos;

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
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public List<int> RoomIds { get; set; } = new List<int>();
        public List<string> RoomNames { get; set; } = new List<string>();
        public List<RoomTypeQuantityDto> RoomTypeDetails { get; set; } = new List<RoomTypeQuantityDto>();
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public int? PaymentStatusId{ get; set; }
        public int? BookingTypeId { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string BookingType { get; set; } = string.Empty;
        public string? SpecialRequests { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? PaymentUrl { get; set; }
        public string OrderCode { get; set; }
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
        public bool IsCancel { get; set; } = false;
        public int? UserId { get; set; }
        public string? CancellationReason { get; set; }
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

    // ===== BOOKING MANAGEMENT - OFFLINE BOOKING MODELS =====

    /// <summary>
    /// Request tạo booking offline - dành cho lễ tán
    /// Lễ tân sẽ tự chọn phòng cụ thể (không tự động)
    /// </summary>
    public class CreateOfflineBookingRequest
    {
        // Customer Info
        /// <summary>
        /// CustomerId nếu đã tìm thấy khách hàng qua Quick Search
        /// Null nếu là khách hàng mới hoàn toàn
        /// </summary>
        public int? CustomerId { get; set; }
        
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? IdentityCard { get; set; }
        public string? Address { get; set; }

        // Booking Info - Lễ tán chọn phòng cụ thể
        public List<int> RoomIds { get; set; } = new List<int>();
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string? SpecialRequests { get; set; }

        // Payment Info
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, Transfer
        public string? PaymentNote { get; set; }
    }

    /// <summary>
    /// Request cập nhật booking offline
    /// </summary>
    public class UpdateOfflineBookingRequest
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? IdentityCard { get; set; }
        public string? Address { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public string? SpecialRequests { get; set; }
    }

    /// <summary>
    /// Request xác nhận đặt cọc offline
    /// </summary>
    public class ConfirmOfflineDepositRequest
    {
        public decimal DepositAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentNote { get; set; }
        public string? TransactionReference { get; set; }
    }

    /// <summary>
    /// Request xác nhận thanh toán toàn bộ offline
    /// </summary>
    public class ConfirmOfflinePaymentRequest
    {
        public decimal PaidAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentNote { get; set; }
        public string? TransactionReference { get; set; }
    }

    /// <summary>
    /// Filter để lọc danh sách booking offline
    /// </summary>
    public class BookingFilterRequest : PagedRequestDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BookingStatus { get; set; }
        public string? key { get; set; }
        public int? BookingType { get; set; }
    }

    /// <summary>
    /// DTO thông tin customer tìm được
    /// </summary>
    public class CustomerInfoDto
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? IdentityCard { get; set; }
        public string? Address { get; set; }
        public int TotalBookings { get; set; }
        public DateTime? LastBookingDate { get; set; }
    }

    /// <summary>
    /// DTO chi tiết booking offline
    /// </summary>
    public class OfflineBookingDto
    {
        public int BookingId { get; set; }
        public CustomerInfoDto Customer { get; set; } = new CustomerInfoDto();
        public List<RoomDto> Rooms { get; set; } = new List<RoomDto>();
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int TotalNights { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string DepositStatus { get; set; } = string.Empty;
        public string? SpecialRequests { get; set; }
        public string CreatedByEmployee { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<PaymentHistoryDto> PaymentHistory { get; set; } = new List<PaymentHistoryDto>();
    }

    /// <summary>
    /// DTO thông tin phòng
    /// </summary>
    public class RoomDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomTypeName { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
    }

    /// <summary>
    /// DTO lịch sử thanh toán
    /// </summary>
    public class PaymentHistoryDto
    {
        public int TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty; // Deposit, FullPayment
        public string? Note { get; set; }
        public string ProcessedBy { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }

    /// <summary>
    /// Request để hủy booking
    /// </summary>
    public class CancelBookingRequest
    {
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO chi tiết booking đầy đủ
    /// </summary>
    public class BookingDetailDto
    {
        public int BookingId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public List<int> RoomIds { get; set; } = new List<int>();
        public List<string> RoomNames { get; set; } = new List<string>();
        public List<RoomTypeQuantityDto> RoomTypeDetails { get; set; } = new List<RoomTypeQuantityDto>();
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string BookingType { get; set; } = string.Empty;
        public string? SpecialRequests { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Request xác nhận thanh toán thủ công (QR/Bank Transfer) - dành cho nhân viên
    /// </summary>
    public class ConfirmManualPaymentRequest
    {
        public int BookingId { get; set; }
        public string TransactionRef { get; set; } = string.Empty;
        public decimal PaidAmount { get; set; }
        public string PaymentMethod { get; set; } = "BankTransfer";
        public string? PaymentNote { get; set; }
        public string? ProofImageUrl { get; set; }
    }

    /// <summary>
    /// Response thông tin QR payment - được tạo tự động từ GenerateVietQRUrl
    /// </summary>
    public class QRPaymentInfoDto
    {
        /// <summary>
        /// URL của QR code image từ VietQR (tự động generate theo số tiền)
        /// Format: https://img.vietqr.io/image/{BANK_CODE}-{ACCOUNT_NUMBER}-{TEMPLATE}.png?amount={AMOUNT}&addInfo={DESCRIPTION}
        /// </summary>
        public string QRCodeUrl { get; set; } = string.Empty;
        
        public string BankName { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string TransactionRef { get; set; } = string.Empty;
        
        /// <summary>
        /// Thông tin text để hiển thị cho khách hàng
        /// </summary>
        public string QRDataText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request tìm kiếm và filter phòng available
    /// </summary>
    public class SearchAvailableRoomsRequest
    {
        /// <summary>
        /// Ngày check-in
        /// </summary>
        public DateTime? CheckInDate { get; set; }
        
        /// <summary>
        /// Ngày check-out
        /// </summary>
        public DateTime? CheckOutDate { get; set; }
        
        /// <summary>
        /// Loại phòng
        /// </summary>
        public int? RoomTypeId { get; set; }
        
        /// <summary>
        /// Số lượng giường
        /// </summary>
        public int? NumberOfBeds { get; set; }
        
        /// <summary>
        /// Loại giường (King, Queen, Twin...)
        /// </summary>
        public string? BedType { get; set; }
        
        /// <summary>
        /// Số người tối đa
        /// </summary>
        public int? MaxOccupancy { get; set; }
        
        /// <summary>
        /// Giá tối thiểu
        /// </summary>
        public decimal? MinPrice { get; set; }
        
        /// <summary>
        /// Giá tối đa
        /// </summary>
        public decimal? MaxPrice { get; set; }
        
        /// <summary>
        /// Diện tích tối thiểu (m2)
        /// </summary>
        public decimal? MinRoomSize { get; set; }
        
        /// <summary>
        /// Tìm kiếm theo tên hoặc mã phòng
        /// </summary>
        public string? SearchTerm { get; set; }
        
        /// <summary>
        /// Sắp xếp theo (Price, RoomSize, RoomName)
        /// </summary>
        public string? SortBy { get; set; }
        
        /// <summary>
        /// Sắp xếp giảm dần
        /// </summary>
        public bool IsDescending { get; set; } = false;
        
        /// <summary>
        /// Trang hiện tại
        /// </summary>
        public int PageNumber { get; set; } = 1;
        
        /// <summary>
        /// Số lượng kết quả mỗi trang
        /// </summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Response danh sách phòng available
    /// </summary>
    public class AvailableRoomsResponse
    {
        public List<AvailableRoomDto> Rooms { get; set; } = new List<AvailableRoomDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// DTO thông tin phòng available
    /// </summary>
    public class AvailableRoomDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public string RoomTypeCode { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public int MaxOccupancy { get; set; }
        public decimal? RoomSize { get; set; }
        public int? NumberOfBeds { get; set; }
        public string? BedType { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Amenities { get; set; } = new List<string>();
        public List<string> Images { get; set; } = new List<string>();
    }

    /// <summary>
    /// Request để thêm dịch vụ vào booking trong quá trình khách ở
    /// </summary>
    public class AddBookingServiceRequest
    {
        /// <summary>
        /// Booking ID
        /// </summary>
        [Required]
        public int BookingId { get; set; }

        /// <summary>
        /// Danh sách dịch vụ cần thêm
        /// </summary>
        [Required]
        public List<BookingServiceItem> Services { get; set; } = new List<BookingServiceItem>();
    }

    /// <summary>
    /// Thông tin dịch vụ cần thêm vào booking
    /// </summary>
    public class BookingServiceItem
    {
        /// <summary>
        /// Service ID
        /// </summary>
        [Required]
        public int ServiceId { get; set; }

        /// <summary>
        /// Số lượng
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        /// <summary>
        /// BookingRoomId nếu dịch vụ gắn với phòng cụ thể (VD: minibar, giặt ủi theo phòng)
        /// Null nếu là dịch vụ chung cho booking (VD: spa, massage)
        /// </summary>
        public int? BookingRoomId { get; set; }

        /// <summary>
        /// Ghi chú cho dịch vụ
        /// </summary>
        public string? Note { get; set; }
    }
}
