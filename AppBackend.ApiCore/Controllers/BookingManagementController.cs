using AppBackend.Services.ApiModels.BookingModel;
using AppBackend.Services.Services.BookingServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// API quản lý booking offline - dành cho Lễ tân, Manager, Admin
    /// Lễ tân tự chọn phòng cụ thể, không tự động chọn phòng
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BookingManagementController : BaseApiController
    {
        private readonly IBookingManagementService _bookingManagementService;

        public BookingManagementController(IBookingManagementService bookingManagementService)
        {
            _bookingManagementService = bookingManagementService;
        }

        /// <summary>
        /// Search và filter phòng available - API GET chuẩn
        /// </summary>
        /// <remarks>
        /// API GET để tìm kiếm và filter phòng trống với đầy đủ tiêu chí:
        /// - Ngày check-in/check-out
        /// - Loại phòng (RoomType)
        /// - Số lượng giường
        /// - Loại giường (King, Queen, Twin...)
        /// - Số người tối đa
        /// - Khoảng giá
        /// - Diện tích phòng
        /// - Tìm kiếm theo tên/mã phòng
        /// - Sắp xếp theo giá, diện tích, tên
        /// 
        /// ### Query Parameters Examples:
        /// 
        /// **Tìm phòng trống trong khoảng thời gian:**
        /// ```
        /// GET /api/BookingManagement/rooms/search?checkInDate=2025-12-10&checkOutDate=2025-12-12
        /// ```
        /// 
        /// **Filter theo loại phòng và số giường:**
        /// ```
        /// GET /api/BookingManagement/rooms/search?roomTypeId=1&numberOfBeds=2
        /// ```
        /// 
        /// **Filter theo giá:**
        /// ```
        /// GET /api/BookingManagement/rooms/search?minPrice=500000&maxPrice=2000000
        /// ```
        /// 
        /// **Filter theo loại giường:**
        /// ```
        /// GET /api/BookingManagement/rooms/search?bedType=King
        /// ```
        /// 
        /// **Filter theo số người:**
        /// ```
        /// GET /api/BookingManagement/rooms/search?maxOccupancy=4
        /// ```
        /// 
        /// **Search theo tên:**
        /// ```
        /// GET /api/BookingManagement/rooms/search?searchTerm=deluxe
        /// ```
        /// 
        /// **Kết hợp nhiều filter:**
        /// ```
        /// GET /api/BookingManagement/rooms/search?checkInDate=2025-12-10&checkOutDate=2025-12-12&roomTypeId=1&numberOfBeds=2&minPrice=1000000&maxPrice=3000000&sortBy=price&pageNumber=1&pageSize=10
        /// ```
        /// 
        /// ### Sorting Options:
        /// - `sortBy=price` - Sắp xếp theo giá
        /// - `sortBy=roomsize` - Sắp xếp theo diện tích
        /// - `sortBy=roomname` - Sắp xếp theo tên phòng
        /// - `isDescending=true` - Sắp xếp giảm dần
        /// 
        /// ### Response bao gồm:
        /// - Danh sách phòng chi tiết (Room info, RoomType, Price, Size, Beds...)
        /// - Amenities của từng phòng
        /// - Hình ảnh phòng
        /// - Pagination info
        /// </remarks>
        [HttpGet("rooms/search")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> SearchAvailableRooms([FromQuery] SearchAvailableRoomsRequest request)
        {
            var result = await _bookingManagementService.SearchAvailableRoomsAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tạo booking offline - Lễ tân tự chọn phòng cụ thể
        /// </summary>
        /// <remarks>
        /// ### Đặc điểm:
        /// - Lễ tân **tự chọn các phòng cụ thể** bằng RoomIds (không tự động)
        /// - BookingType = "WalkIn"
        /// - Status mặc định = "CheckedIn"
        /// - Thanh toán toàn bộ ngay tại quầy
        /// - Tự động gửi email xác nhận
        /// 
        /// ### Request Example:
        /// ```json
        /// {
        ///   "fullName": "Nguyễn Văn A",
        ///   "email": "nguyenvana@gmail.com",
        ///   "phoneNumber": "0901234567",
        ///   "identityCard": "001234567890",
        ///   "address": "123 Đường ABC, TP.HCM",
        ///   "roomIds": [101, 102, 201],
        ///   "checkInDate": "2025-12-10T14:00:00Z",
        ///   "checkOutDate": "2025-12-12T12:00:00Z",
        ///   "specialRequests": "Phòng tầng cao, view đẹp",
        ///   "paymentMethod": "Cash"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("offline")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> CreateOfflineBooking([FromBody] CreateOfflineBookingRequest request)
        {
            var employeeId = CurrentUserId;
            var result = await _bookingManagementService.CreateOfflineBookingAsync(request, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật thông tin booking offline
        /// </summary>
        [HttpPut("offline/{bookingId}")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> UpdateOfflineBooking(int bookingId, [FromBody] UpdateOfflineBookingRequest request)
        {
            var employeeId = CurrentUserId;
            var result = await _bookingManagementService.UpdateOfflineBookingAsync(bookingId, request, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách booking offline với filter
        /// </summary>
        /// <remarks>
        /// ### Query Parameters:
        /// - fromDate: Lọc từ ngày
        /// - toDate: Lọc đến ngày
        /// - bookingStatus: Lọc theo trạng thái
        /// - pageNumber: Trang hiện tại (default: 1)
        /// - pageSize: Số records mỗi trang (default: 20)
        /// </remarks>
        [HttpGet("offline")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> GetOfflineBookings([FromQuery] OfflineBookingFilterRequest filter)
        {
            var result = await _bookingManagementService.GetOfflineBookingsAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy chi tiết một booking
        /// </summary>
        [HttpGet("{bookingId}")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> GetBookingDetail(int bookingId)
        {
            var result = await _bookingManagementService.GetBookingDetailAsync(bookingId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Hủy booking với lý do
        /// </summary>
        /// <remarks>
        /// ### Request Example:
        /// ```json
        /// {
        ///   "reason": "Khách yêu cầu hủy do thay đổi lịch trình"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("{bookingId}/cancel")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> CancelBooking(int bookingId, [FromBody] CancelBookingRequest request)
        {
            var employeeId = CurrentUserId;
            var result = await _bookingManagementService.CancelBookingAsync(bookingId, request, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy thông tin QR payment cho booking
        /// </summary>
        /// <remarks>
        /// Nhân viên có thể generate QR code để khách thanh toán qua VietQR
        /// Chỉ áp dụng cho booking có status = "Pending"
        /// </remarks>
        [HttpGet("{bookingId}/qr-payment")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> GetQRPaymentInfo(int bookingId)
        {
            var result = await _bookingManagementService.GetQRPaymentInfoAsync(bookingId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Manager/Admin xác nhận đã nhận được tiền cọc từ khách (sau khi check bill ngân hàng)
        /// </summary>
        /// <remarks>
        /// ### Luồng xử lý:
        /// 1. Khách báo "đã chuyển khoản" → Status chuyển sang **PendingConfirmation**
        /// 2. Manager vào app ngân hàng kiểm tra bill
        /// 3. Manager gọi API này để xác nhận → Status chuyển sang **Confirmed**
        /// 4. Hệ thống tự động:
        ///    - Tạo transaction record
        ///    - **Gửi email cảm ơn + thông tin đặt phòng chi tiết cho khách**
        ///    - Email bao gồm: Booking ID, thông tin phòng, ngày check-in/out, tổng tiền, link xem chi tiết
        /// 
        /// ### Authorization:
        /// - Chỉ **Manager** và **Admin** mới có quyền confirm
        /// - Lễ tân (Receptionist) **KHÔNG** có quyền confirm payment
        /// 
        /// ### Example:
        /// ```
        /// POST /api/BookingManagement/123/confirm-payment
        /// ```
        /// 
        /// ### Response Success:
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "statusCode": 200,
        ///   "message": "Xác nhận thanh toán thành công. Email đã được gửi đến khách hàng."
        /// }
        /// ```
        /// </remarks>
        [HttpPost("{bookingId}/confirm-payment")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> ConfirmPayment(int bookingId)
        {
            // Gọi service ConfirmOnlineBookingAsync đã có sẵn trong BookingService
            var result = await _bookingManagementService.ConfirmOnlineBookingAsync(bookingId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
