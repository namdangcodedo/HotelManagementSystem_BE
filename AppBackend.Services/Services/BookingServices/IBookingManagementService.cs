using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.BookingModel;

namespace AppBackend.Services.Services.BookingServices
{
    public interface IBookingManagementService
    {
        // Offline Booking Methods
        Task<ResultModel> CreateOfflineBookingAsync(CreateOfflineBookingRequest request, int employeeId);
        Task<ResultModel> UpdateOfflineBookingAsync(int bookingId, UpdateOfflineBookingRequest request, int employeeId);
        Task<ResultModel> GetOfflineBookingsAsync(OfflineBookingFilterRequest filter);
        
        // General Booking Methods
        Task<ResultModel> GetBookingDetailAsync(int bookingId);
        Task<ResultModel> CancelBookingAsync(int bookingId, CancelBookingRequest request, int employeeId);
        
        // QR Payment
        Task<ResultModel> GetQRPaymentInfoAsync(int bookingId);
        
        // Search Available Rooms
        Task<ResultModel> SearchAvailableRoomsAsync(SearchAvailableRoomsRequest request);
        
        // Confirm Online Booking Payment (Manager/Admin only)
        Task<ResultModel> ConfirmOnlineBookingAsync(int bookingId);
    }
}