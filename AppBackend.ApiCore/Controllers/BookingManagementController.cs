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
            var employeeId = CurrentUserId;

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
            var employeeId = CurrentUserId;

            var result = await _bookingManagementService.UpdateOfflineBookingAsync(bookingId, request, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xác nhận thanh toán đặt cọc cho booking offline (tiền mặt/chuyển khoản)
        /// </summary>
        [HttpPost("offline-booking/{bookingId}/confirm-deposit")]
        public async Task<IActionResult> ConfirmOfflineDeposit(int bookingId, [FromBody] ConfirmOfflineDepositRequest request)
        {
            var employeeId = CurrentUserId;

            var result = await _bookingManagementService.ConfirmOfflineDepositAsync(bookingId, request, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xác nhận thanh toán toàn bộ cho booking offline và gửi email cảm ơn
        /// </summary>
        [HttpPost("offline-booking/{bookingId}/confirm-payment")]
        public async Task<IActionResult> ConfirmOfflinePayment(int bookingId, [FromBody] ConfirmOfflinePaymentRequest request)
        {
            var employeeId = CurrentUserId;

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
            var employeeId = CurrentUserId;

            var result = await _bookingManagementService.CancelOfflineBookingAsync(bookingId, request.Reason ?? "", employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách tất cả booking với filter và phân trang (Online + Offline)
        /// </summary>
        /// <remarks>
        /// ### API tổng hợp cho quản lý tất cả booking
        /// Hỗ trợ filter theo nhiều tiêu chí:
        /// - Khoảng thời gian (fromDate, toDate)
        /// - Trạng thái booking (bookingStatus)
        /// - Trạng thái thanh toán (paymentStatus)
        /// - Trạng thái đặt cọc (depositStatus)
        /// - Loại booking (bookingType: Online, Walkin)
        /// - Tìm kiếm khách hàng (customerName, phoneNumber, email)
        /// - Tìm theo mã booking (bookingCode)
        /// - Sắp xếp (sortBy: CreatedAt, CheckInDate, TotalAmount)
        /// 
        /// ### Query Parameters Example:
        /// ```
        /// GET /api/bookingmanagement/bookings?pageNumber=1&pageSize=20&fromDate=2024-01-01&bookingType=Online&sortBy=CheckInDate&isDescending=true
        /// ```
        /// </remarks>
        [HttpGet("bookings")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> GetBookingList([FromQuery] GetBookingListRequest request)
        {
            var result = await _bookingManagementService.GetBookingListAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy chi tiết đầy đủ của một booking
        /// </summary>
        /// <remarks>
        /// ### Thông tin chi tiết bao gồm:
        /// - Thông tin khách hàng đầy đủ (lịch sử booking, tổng chi tiêu)
        /// - Danh sách phòng với ảnh và tiện nghi
        /// - Lịch sử thanh toán chi tiết
        /// - Lịch sử thay đổi booking
        /// - Thông tin nhân viên tạo/cập nhật
        /// </remarks>
        [HttpGet("booking/{bookingId}/detail")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> GetBookingDetail(int bookingId)
        {
            var result = await _bookingManagementService.GetBookingDetailAsync(bookingId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật trạng thái booking
        /// </summary>
        /// <remarks>
        /// ### Các trạng thái hợp lệ:
        /// - Confirmed: Xác nhận booking
        /// - CheckedIn: Khách đã check-in
        /// - CheckedOut: Khách đã check-out
        /// - Cancelled: Hủy booking
        /// 
        /// ### Request Example:
        /// ```json
        /// {
        ///   "status": "CheckedIn",
        ///   "note": "Khách đã check-in lúc 14:00"
        /// }
        /// ```
        /// </remarks>
        [HttpPut("booking/{bookingId}/status")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, [FromBody] UpdateBookingStatusRequest request)
        {
            var employeeId = CurrentUserId;

            var result = await _bookingManagementService.UpdateBookingStatusAsync(bookingId, request, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Hủy booking với lý do
        /// </summary>
        /// <remarks>
        /// ### Lưu ý:
        /// - Chỉ có thể hủy booking chưa check-in
        /// - Lý do hủy sẽ được lưu vào SpecialRequests
        /// - Phòng sẽ được mở khóa tự động
        /// 
        /// ### Request Example:
        /// ```json
        /// {
        ///   "reason": "Khách yêu cầu hủy do thay đổi lịch trình"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("booking/{bookingId}/cancel")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> CancelBooking(int bookingId, [FromBody] CancelBookingRequest request)
        {
            var employeeId = CurrentUserId;

            var result = await _bookingManagementService.CancelBookingAsync(bookingId, request, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy thống kê booking theo khoảng thời gian
        /// </summary>
        /// <remarks>
        /// ### Thống kê bao gồm:
        /// - Tổng số booking (online + offline)
        /// - Tổng doanh thu
        /// - Giá trị trung bình mỗi booking
        /// - Số booking đã xác nhận/hủy
        /// - Thống kê theo ngày/tuần/tháng/năm
        /// 
        /// ### Query Parameters:
        /// - fromDate: Ngày bắt đầu
        /// - toDate: Ngày kết thúc
        /// - groupBy: day | week | month | year
        /// 
        /// ### Example:
        /// ```
        /// GET /api/bookingmanagement/statistics?fromDate=2024-01-01&toDate=2024-12-31&groupBy=month
        /// ```
        /// </remarks>
        [HttpGet("statistics")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> GetBookingStatistics([FromQuery] BookingStatisticsRequest request)
        {
            var result = await _bookingManagementService.GetBookingStatisticsAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gửi lại email xác nhận booking cho khách
        /// </summary>
        [HttpPost("booking/{bookingId}/resend-confirmation")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> ResendBookingConfirmation(int bookingId)
        {
            var result = await _bookingManagementService.ResendBookingConfirmationEmailAsync(bookingId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
