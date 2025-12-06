using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.RoomManagement;

namespace AppBackend.Services.Services.RoomManagement
{
    public interface IRoomManagementService
    {
        /// <summary>
        /// Lấy danh sách phòng với tìm kiếm và phân trang
        /// </summary>
        Task<ResultModel> SearchRoomsAsync(SearchRoomsRequest request);

        /// <summary>
        /// Lấy sơ đồ phòng theo tầng
        /// </summary>
        Task<ResultModel> GetRoomMapAsync(int? floor = null);

        /// <summary>
        /// Lấy chi tiết một phòng
        /// </summary>
        Task<ResultModel> GetRoomDetailAsync(int roomId);

        /// <summary>
        /// Thay đổi trạng thái phòng (Manager/Receptionist)
        /// </summary>
        Task<ResultModel> ChangeRoomStatusAsync(ChangeRoomStatusRequest request, int userId, string userRole);

        /// <summary>
        /// Thay đổi trạng thái nhiều phòng cùng lúc (Manager only)
        /// </summary>
        Task<ResultModel> BulkChangeRoomStatusAsync(BulkChangeRoomStatusRequest request, int userId);

        /// <summary>
        /// Đánh dấu phòng đang dọn dẹp (Housekeeper)
        /// </summary>
        Task<ResultModel> MarkRoomAsCleaningAsync(int roomId, int housekeeperId);

        /// <summary>
        /// Đánh dấu phòng dọn xong (Housekeeper)
        /// </summary>
        Task<ResultModel> MarkRoomAsCleanedAsync(int roomId, int housekeeperId);

        /// <summary>
        /// Đánh dấu phòng cần bảo trì (Technician/Manager)
        /// </summary>
        Task<ResultModel> MarkRoomForMaintenanceAsync(int roomId, int userId, string reason);

        /// <summary>
        /// Đánh dấu phòng hoàn tất bảo trì (Technician/Manager)
        /// </summary>
        Task<ResultModel> CompleteMaintenanceAsync(int roomId, int userId);

        /// <summary>
        /// Lấy thống kê trạng thái phòng
        /// </summary>
        Task<ResultModel> GetRoomStatusSummaryAsync();

        /// <summary>
        /// Lấy danh sách trạng thái phòng có thể chuyển đổi (theo role)
        /// </summary>
        Task<ResultModel> GetAvailableStatusTransitionsAsync(int roomId, string userRole);
    }
}

