using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.BookingModel;

namespace AppBackend.Services.Services.BookingServices
{
    public interface IBookingService
    {
        Task<ResultModel> CheckRoomAvailabilityAsync(CheckRoomAvailabilityRequest request);
        Task<ResultModel> CreateBookingAsync(CreateBookingRequest request, int userId);
        Task<ResultModel> CreateGuestBookingAsync(CreateGuestBookingRequest request);
        Task<ResultModel> GetBookingByIdAsync(int bookingId);
        Task<ResultModel> GetBookingByTokenAsync(string token);
        Task<ResultModel> ConfirmPaymentAsync(ConfirmPaymentRequest request);
        Task<ResultModel> CancelBookingAsync(int bookingId, int userId);
        Task<ResultModel> GetMyBookingsAsync(int customerId);
        Task<ResultModel> GetMyBookingsByAccountIdAsync(int accountId);
        Task<ResultModel> HandlePayOSWebhookAsync(PayOSWebhookRequest request);
        Task<ResultModel> CancelBookingCacheAsync(int bookingId);
    }
}
