using AppBackend.Services.ApiModels.BookingModel;
using AppBackend.Services.Services.BookingServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs quản lý đặt phòng với cache locking và message queue
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : BaseApiController
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        /// <summary>
        /// Kiểm tra phòng có sẵn để đặt không
        /// </summary>
        [HttpPost("check-availability")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckAvailability([FromBody] CheckRoomAvailabilityRequest request)
        {
            var result = await _bookingService.CheckRoomAvailabilityAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tạo booking mới - Trả về BookingId và PayOS payment URL
        /// </summary>
        /// <remarks>
        /// Luồng xử lý:
        /// 1. Lock các phòng trong cache (10 phút)
        /// 2. Tạo booking trong database
        /// 3. Tạo PayOS payment link
        /// 4. Trả về BookingId và Payment URL
        /// 5. Tự động hủy booking sau 15 phút nếu chưa thanh toán
        /// </remarks>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _bookingService.CreateBookingAsync(request, userId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tạo booking cho GUEST (không cần đăng nhập/đăng ký tài khoản)
        /// </summary>
        /// <remarks>
        /// Guest Booking Flow:
        /// 1. Người dùng chỉ cần nhập thông tin: Họ tên, Email, SĐT, CMND (optional), Địa chỉ (optional)
        /// 2. Hệ thống tự động tạo/tìm Customer record dựa trên Email hoặc SĐT
        /// 3. Lock phòng, tạo booking và generate payment link
        /// 4. Guest thanh toán và hoàn tất booking mà không cần tài khoản
        /// 
        /// Ưu điểm:
        /// - Không cần đăng ký tài khoản
        /// - Đặt phòng nhanh chóng
        /// - Tự động lưu lịch sử booking theo email/phone để lần sau dễ dàng hơn
        /// </remarks>
        [HttpPost("guest")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateGuestBooking([FromBody] CreateGuestBookingRequest request)
        {
            var result = await _bookingService.CreateGuestBookingAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết booking
        /// </summary>
        [HttpGet("{bookingId}")]
        [Authorize]
        public async Task<IActionResult> GetBookingById(int bookingId)
        {
            var result = await _bookingService.GetBookingByIdAsync(bookingId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xác nhận thanh toán từ PayOS callback
        /// </summary>
        /// <remarks>
        /// Được gọi từ PayOS sau khi thanh toán thành công
        /// </remarks>
        [HttpPost("confirm-payment")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
        {
            var result = await _bookingService.ConfirmPaymentAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Hủy booking
        /// </summary>
        [HttpDelete("{bookingId}")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _bookingService.CancelBookingAsync(bookingId, userId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách booking của khách hàng hiện tại
        /// </summary>
        [HttpGet("my-bookings")]
        [Authorize]
        public async Task<IActionResult> GetMyBookings()
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _bookingService.GetMyBookingsByAccountIdAsync(accountId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
