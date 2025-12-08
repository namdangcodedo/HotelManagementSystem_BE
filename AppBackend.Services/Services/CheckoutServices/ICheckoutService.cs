using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.BookingModel;

namespace AppBackend.Services.Services.CheckoutServices
{
    /// <summary>
    /// Service interface cho checkout - thanh toán và hoàn tất booking
    /// </summary>
    public interface ICheckoutService
    {
        /// <summary>
        /// Xem trước hóa đơn checkout (preview) - không lưu database
        /// Hiển thị chi tiết: tiền phòng, tiền dịch vụ, tiền cọc, còn phải trả
        /// </summary>
        /// <param name="request">Thông tin preview checkout</param>
        /// <returns>Chi tiết hóa đơn dự kiến</returns>
        Task<ResultModel> PreviewCheckoutAsync(PreviewCheckoutRequest request);

        /// <summary>
        /// Xử lý checkout và thanh toán hoàn tất
        /// - Tính tiền phòng (theo ngày thực tế nếu offline, hoặc theo booking nếu online)
        /// - Tính tiền dịch vụ đã sử dụng
        /// - Trừ tiền cọc nếu booking online
        /// - Tạo transaction thanh toán
        /// - Cập nhật booking status = Completed
        /// - Cập nhật room status = Available
        /// </summary>
        /// <param name="request">Thông tin checkout</param>
        /// <param name="processedBy">User ID của nhân viên xử lý</param>
        /// <returns>Kết quả checkout với chi tiết hóa đơn</returns>
        Task<ResultModel> ProcessCheckoutAsync(CheckoutRequest request, int? processedBy = null);

        /// <summary>
        /// Lấy thông tin chi tiết booking để chuẩn bị checkout
        /// </summary>
        /// <param name="bookingId">Booking ID</param>
        /// <returns>Thông tin booking với rooms, services, transactions</returns>
        Task<ResultModel> GetBookingForCheckoutAsync(int bookingId);
    }
}
