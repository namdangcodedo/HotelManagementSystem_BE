using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.BookingModel;

namespace AppBackend.Services.Services.BookingServices
{
    /// <summary>
    /// Service quản lý booking offline - dành cho Lễ tân, Manager, Admin
    /// Tự động điền thông tin customer từ email/số điện thoại
    /// Gửi email cảm ơn sau khi booking thành công
    /// </summary>
    public interface IBookingManagementService
    {
        /// <summary>
        /// Tìm customer theo email hoặc số điện thoại để tự động điền thông tin
        /// </summary>
        /// <param name="searchTerm">Email hoặc số điện thoại</param>
        /// <returns>Thông tin customer nếu tìm thấy</returns>
        Task<ResultModel> SearchCustomerAsync(string searchTerm);

        /// <summary>
        /// Tạo booking offline cho khách - lễ tân nhập thông tin
        /// Nếu customer đã tồn tại (theo email/SĐT) thì dùng thông tin cũ
        /// Nếu chưa có thì tạo mới
        /// </summary>
        /// <param name="request">Thông tin booking offline</param>
        /// <param name="employeeId">ID nhân viên tạo booking</param>
        /// <returns>Thông tin booking đã tạo</returns>
        Task<ResultModel> CreateOfflineBookingAsync(CreateOfflineBookingRequest request, int employeeId);

        /// <summary>
        /// Cập nhật thông tin booking offline (trước khi check-in)
        /// </summary>
        /// <param name="bookingId">ID booking cần cập nhật</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <param name="employeeId">ID nhân viên thực hiện</param>
        /// <returns>Kết quả cập nhật</returns>
        Task<ResultModel> UpdateOfflineBookingAsync(int bookingId, UpdateOfflineBookingRequest request, int employeeId);

        /// <summary>
        /// Xác nhận thanh toán đặt cọc cho booking offline
        /// </summary>
        /// <param name="bookingId">ID booking</param>
        /// <param name="request">Thông tin thanh toán</param>
        /// <param name="employeeId">ID nhân viên xác nhận</param>
        /// <returns>Kết quả xác nhận</returns>
        Task<ResultModel> ConfirmOfflineDepositAsync(int bookingId, ConfirmOfflineDepositRequest request, int employeeId);

        /// <summary>
        /// Xác nhận thanh toán toàn bộ cho booking offline
        /// </summary>
        /// <param name="bookingId">ID booking</param>
        /// <param name="request">Thông tin thanh toán</param>
        /// <param name="employeeId">ID nhân viên xác nhận</param>
        /// <returns>Kết quả xác nhận và gửi email cảm ơn</returns>
        Task<ResultModel> ConfirmOfflinePaymentAsync(int bookingId, ConfirmOfflinePaymentRequest request, int employeeId);

        /// <summary>
        /// Lấy danh sách booking offline theo bộ lọc
        /// </summary>
        /// <param name="filter">Bộ lọc: ngày, trạng thái, customer...</param>
        /// <returns>Danh sách booking</returns>
        Task<ResultModel> GetOfflineBookingsAsync(OfflineBookingFilterRequest filter);

        /// <summary>
        /// Hủy booking offline
        /// </summary>
        /// <param name="bookingId">ID booking cần hủy</param>
        /// <param name="reason">Lý do hủy</param>
        /// <param name="employeeId">ID nhân viên thực hiện</param>
        /// <returns>Kết quả hủy</returns>
        Task<ResultModel> CancelOfflineBookingAsync(int bookingId, string reason, int employeeId);

        /// <summary>
        /// Gửi lại email xác nhận booking cho khách
        /// </summary>
        /// <param name="bookingId">ID booking</param>
        /// <returns>Kết quả gửi email</returns>
        Task<ResultModel> ResendBookingConfirmationEmailAsync(int bookingId);
    }
}
