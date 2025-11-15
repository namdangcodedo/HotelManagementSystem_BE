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
    [Authorize(Roles = "Receptionist,Manager,Admin")]
    public class BookingManagementController : BaseApiController
    {
        private readonly IBookingManagementService _bookingManagementService;

        public BookingManagementController(IBookingManagementService bookingManagementService)
        {
            _bookingManagementService = bookingManagementService;
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
        /// </summary>
        [HttpPost("offline-booking")]
        public async Task<IActionResult> CreateOfflineBooking([FromBody] CreateOfflineBookingRequest request)
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            {
                return Unauthorized("Không tìm thấy thông tin nhân viên");
            }

            var result = await _bookingManagementService.CreateOfflineBookingAsync(request, employeeId);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật thông tin booking offline (trước khi check-in)
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
            return Ok(result);
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
            return Ok(result);
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
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách booking offline với bộ lọc
        /// </summary>
        [HttpGet("offline-bookings")]
        public async Task<IActionResult> GetOfflineBookings([FromQuery] OfflineBookingFilterRequest filter)
        {
            var result = await _bookingManagementService.GetOfflineBookingsAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Hủy booking offline
        /// </summary>
        [HttpDelete("offline-booking/{bookingId}")]
        public async Task<IActionResult> CancelOfflineBooking(int bookingId, [FromBody] string reason)
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
            if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            {
                return Unauthorized("Không tìm thấy thông tin nhân viên");
            }

            var result = await _bookingManagementService.CancelOfflineBookingAsync(bookingId, reason, employeeId);
            return Ok(result);
        }

        /// <summary>
        /// Gửi lại email xác nhận booking cho khách
        /// </summary>
        [HttpPost("offline-booking/{bookingId}/resend-email")]
        public async Task<IActionResult> ResendBookingConfirmationEmail(int bookingId)
        {
            var result = await _bookingManagementService.ResendBookingConfirmationEmailAsync(bookingId);
            return Ok(result);
        }
    }
}
