using AppBackend.Services.ApiModels.BookingModel;
using AppBackend.Services.Services.BookingServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : BaseApiController
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        
        [HttpPost("check-availability")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckAvailability([FromBody] CheckRoomAvailabilityRequest request)
        {
            var result = await _bookingService.CheckRoomAvailabilityAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            var userId = CurrentUserId;
            var result = await _bookingService.CreateBookingAsync(request, userId);
            return StatusCode(result.StatusCode, result);
        }

        
        [HttpPost("guest")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateGuestBooking([FromBody] CreateGuestBookingRequest request)
        {
            var result = await _bookingService.CreateGuestBookingAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{bookingId}")]
        [Authorize]
        public async Task<IActionResult> GetBookingById(int bookingId)
        {
            var result = await _bookingService.GetBookingByIdAsync(bookingId);
            return StatusCode(result.StatusCode, result);
        }

        
        [HttpGet("mybooking/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBookingByToken(string token)
        {
            var result = await _bookingService.GetBookingByTokenAsync(token);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("confirm-payment")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
        {
            var userId = CurrentUserId;
            request.UserId = userId;
            var result = await _bookingService.ProcessPaymentAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{bookingId}")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var userId = CurrentUserId;
            var request = new ConfirmPaymentRequest 
            { 
                BookingId = bookingId, 
                IsCancel = true, 
                UserId = userId 
            };
            var result = await _bookingService.ProcessPaymentAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("my-bookings")]
        [Authorize]
        public async Task<IActionResult> GetMyBookings()
        {
            var accountId = CurrentUserId;
            var result = await _bookingService.GetMyBookingsByAccountIdAsync(accountId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
