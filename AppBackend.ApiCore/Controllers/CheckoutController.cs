using AppBackend.Services.ApiModels.BookingModel;
using AppBackend.Services.Services.CheckoutServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// Controller xử lý checkout - thanh toán và hoàn tất booking
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CheckoutController : BaseApiController
    {
        private readonly ICheckoutService _checkoutService;

        public CheckoutController(ICheckoutService checkoutService)
        {
            _checkoutService = checkoutService;
        }

        /// <summary>
        /// Preview hóa đơn checkout (không lưu DB) - Xem trước chi tiết thanh toán
        /// GET: api/checkout/preview/{bookingId}
        /// </summary>
        /// <param name="bookingId">Booking ID</param>
        /// <returns>Chi tiết hóa đơn preview</returns>
        [HttpGet("preview/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> PreviewCheckout(int bookingId)
        {
            var request = new PreviewCheckoutRequest
            {
                BookingId = bookingId
            };

            var result = await _checkoutService.PreviewCheckoutAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xử lý checkout và thanh toán hoàn tất
        /// POST: api/checkout
        /// </summary>
        /// <param name="request">Thông tin checkout</param>
        /// <returns>Kết quả checkout với chi tiết hóa đơn</returns>
        [HttpPost]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> ProcessCheckout([FromBody] CheckoutRequest request)
        {
            // Validate request
            if (request == null)
            {
                return ValidationError("Request không hợp lệ");
            }

            if (request.BookingId <= 0)
            {
                return ValidationError("Booking ID không hợp lệ");
            }

            if (request.PaymentMethodId <= 0)
            {
                return ValidationError("Phương thức thanh toán không hợp lệ");
            }

            // Get current user ID (employee processing checkout)
            var processedBy = CurrentUserId > 0 ? CurrentUserId : (int?)null;

            var result = await _checkoutService.ProcessCheckoutAsync(request, processedBy);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy thông tin booking để chuẩn bị checkout
        /// GET: api/checkout/booking/{bookingId}
        /// </summary>
        /// <param name="bookingId">Booking ID</param>
        /// <returns>Thông tin chi tiết booking</returns>
        [HttpGet("booking/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> GetBookingForCheckout(int bookingId)
        {
            if (bookingId <= 0)
            {
                return ValidationError("Booking ID không hợp lệ");
            }

            var result = await _checkoutService.GetBookingForCheckoutAsync(bookingId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
