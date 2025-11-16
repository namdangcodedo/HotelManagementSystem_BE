using AppBackend.Services.ApiModels.BookingModel;
using AppBackend.Services.Services.BookingServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// API quản lý booking offline - dành cho Lễ tân, Manager, Admin
    /// Tự động điền thông tin customer từ email/SĐT, gửi email cảm ơn sau booking
    /// </summary>
    [Route("api/[controller]")]
    [Authorize(Roles = "Receptionist,Manager,Admin")]
    public class BookingManagementController : BaseApiController
    {
        private readonly IBookingManagementService _bookingManagementService;
        private readonly IBookingService _bookingService;

        public BookingManagementController(IBookingManagementService bookingManagementService, IBookingService bookingService)
        {
            _bookingManagementService = bookingManagementService;
            _bookingService = bookingService;
        }

        /// <summary>
        /// Kiểm tra phòng available theo filter - dành cho lễ tân
        /// Tự động check phòng trong DB và phòng đang bị lock trong cache
        /// </summary>
        /// <remarks>
        /// API này sử dụng lại logic check availability từ BookingService
        /// Phân biệt với booking online: Lễ tân có thể xem chi tiết phòng nào available cụ thể
        /// 
        /// ### Request Example:
        /// ```json
        /// {
        ///   "roomTypes": [
        ///     { "roomTypeId": 1, "quantity": 2 },
        ///     { "roomTypeId": 3, "quantity": 1 }
        ///   ],
        ///   "checkInDate": "2025-11-20T14:00:00Z",
        ///   "checkOutDate": "2025-11-22T12:00:00Z"
        /// }
        /// ```
        /// 
        /// ### Response bao gồm:
        /// - Danh sách room types với số lượng available
        /// - Thông tin chi tiết từng loại phòng
        /// - Các phòng đang bị lock trong cache sẽ không hiển thị
        /// </remarks>
        [HttpPost("available-rooms")]
        public async Task<IActionResult> GetAvailableRooms([FromBody] CheckRoomAvailabilityRequest request)
        {
            var result = await _bookingService.CheckRoomAvailabilityAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tìm kiếm customer theo email hoặc số điện thoại để tự động điền form
        /// </summary>
        /// <param name="searchTerm">Email hoặc số điện thoại</param>
        /// <returns>Thông tin customer nếu tìm thấy</returns>
        [HttpGet("search-customer")]
        public async Task<IActionResult> SearchCustomer([FromQuery] string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest("Vui lòng nhập email hoặc số điện thoại");
            }

            var result = await _bookingManagementService.SearchCustomerAsync(searchTerm);
            return Ok(result);
        }

        /// <summary>
        /// Tạo booking offline cho khách - Lễ tân nhập thông tin tại quầy
        /// Tự động tìm customer theo email/SĐT, nếu chưa có thì tạo mới
        /// BookingType sẽ được set là "Walkin" để phân biệt với booking online
        /// </summary>
        /// <remarks>
        /// ### Đặc điểm booking offline (Walkin):
        /// - BookingType = "Walkin" (khác với "Online")
        /// - Có thể nhận deposit ngay tại quầy (Cash/Card/Transfer)
        /// - Lễ tân có thể sửa thông tin trước khi check-in
        /// - Không cần qua payment gateway như PayOS
        /// 
        /// ### Request Example:
        /// ```json
        /// {
        ///   "fullName": "Nguyễn Văn A",
        ///   "email": "nguyenvana@gmail.com",
        ///   "phoneNumber": "0901234567",
        ///   "identityCard": "001234567890",
        ///   "address": "123 Đường ABC, TP.HCM",
        ///   "roomTypes": [
        ///     { "roomTypeId": 1, "quantity": 1 }
        ///   ],
        ///   "checkInDate": "2025-11-20T14:00:00Z",
        ///   "checkOutDate": "2025-11-22T12:00:00Z",
        ///   "specialRequests": "Tầng cao, view biển",
        ///   "depositAmount": 500000,
        ///   "paymentMethod": "Cash",
        ///   "paymentNote": "Khách đặt cọc tiền mặt"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("offline-booking")]
        public async Task<IActionResult> CreateOfflineBooking([FromBody] CreateOfflineBookingRequest request)
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            {
                return Unauthorized("Không tìm thấy thông tin nhân viên");
            }

            var result = await _bookingManagementService.CreateOfflineBookingAsync(request, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật thông tin booking offline (trước khi check-in)
        /// Chỉ cho phép cập nhật booking có BookingType = "Walkin"
        /// </summary>
        [HttpPut("offline-booking/{bookingId}")]
        public async Task<IActionResult> UpdateOfflineBooking(int bookingId, [FromBody] UpdateOfflineBookingRequest request)
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            {
                return Unauthorized("Không tìm thấy thông tin nhân viên");
            }

            var result = await _bookingManagementService.UpdateOfflineBookingAsync(bookingId, request, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xác nhận thanh toán đặt cọc cho booking offline (tiền mặt/chuyển khoản)
        /// </summary>
        [HttpPost("offline-booking/{bookingId}/confirm-deposit")]
        public async Task<IActionResult> ConfirmOfflineDeposit(int bookingId, [FromBody] ConfirmOfflineDepositRequest request)
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            {
                return Unauthorized("Không tìm thấy thông tin nhân viên");
            }

            var result = await _bookingManagementService.ConfirmOfflineDepositAsync(bookingId, request, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xác nhận thanh toán toàn bộ cho booking offline và gửi email cảm ơn
        /// </summary>
        [HttpPost("offline-booking/{bookingId}/confirm-payment")]
        public async Task<IActionResult> ConfirmOfflinePayment(int bookingId, [FromBody] ConfirmOfflinePaymentRequest request)
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            {
                return Unauthorized("Không tìm thấy thông tin nhân viên");
            }

            var result = await _bookingManagementService.ConfirmOfflinePaymentAsync(bookingId, request, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách booking offline với bộ lọc đầy đủ
        /// </summary>
        /// <remarks>
        /// ### Màn hình quản lý booking sẽ hiển thị:
        /// - Danh sách booking offline (BookingType = "Walkin")
        /// - Filter theo: Ngày, Trạng thái thanh toán, Trạng thái deposit, Tên khách, SĐT
        /// - Phân trang
        /// - Thông tin chi tiết: Khách hàng, Phòng, Giá, Lịch sử thanh toán
        /// 
        /// ### Query Parameters:
        /// - fromDate: Lọc từ ngày check-in
        /// - toDate: Lọc đến ngày check-out
        /// - paymentStatus: "Paid" | "Unpaid" | "PartiallyPaid"
        /// - depositStatus: "Paid" | "Unpaid"
        /// - customerName: Tìm theo tên khách
        /// - phoneNumber: Tìm theo SĐT
        /// - pageNumber: Trang hiện tại (default: 1)
        /// - pageSize: Số records mỗi trang (default: 20)
        /// </remarks>
        [HttpGet("offline-bookings")]
        public async Task<IActionResult> GetOfflineBookings([FromQuery] OfflineBookingFilterRequest filter)
        {
            var result = await _bookingManagementService.GetOfflineBookingsAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy chi tiết một booking offline
        /// </summary>
        [HttpGet("offline-booking/{bookingId}")]
        public async Task<IActionResult> GetOfflineBookingById(int bookingId)
        {
            var result = await _bookingService.GetBookingByIdAsync(bookingId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Hủy booking offline
        /// </summary>
        [HttpDelete("offline-booking/{bookingId}")]
        public async Task<IActionResult> CancelOfflineBooking(int bookingId, [FromBody] CancelBookingRequest request)
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            {
                return Unauthorized("Không tìm thấy thông tin nhân viên");
            }

            var result = await _bookingManagementService.CancelOfflineBookingAsync(bookingId, request.Reason ?? "", employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gửi lại email xác nhận booking cho khách
        /// </summary>
        [HttpPost("offline-booking/{bookingId}/resend-email")]
        public async Task<IActionResult> ResendBookingConfirmationEmail(int bookingId)
        {
            var result = await _bookingManagementService.ResendBookingConfirmationEmailAsync(bookingId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
