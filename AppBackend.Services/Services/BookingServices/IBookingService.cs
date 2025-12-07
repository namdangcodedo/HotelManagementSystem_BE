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
        Task<ResultModel> ProcessPaymentAsync(ConfirmPaymentRequest request);
        Task<ResultModel> GetMyBookingsByAccountIdAsync(int accountId);
        Task<ResultModel> GetQRPaymentInfoAsync(int bookingId);
        Task<ResultModel> ConfirmOnlineBookingAsync(int bookingId);
    }
}
