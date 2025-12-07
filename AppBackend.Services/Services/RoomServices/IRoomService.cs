using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.RoomModel;

namespace AppBackend.Services.Services.RoomServices
{
    public interface IRoomService
    {
        // ============= ROOM TYPE SEARCH (FOR CUSTOMER) =============
        /// <summary>
        /// Tìm kiếm loại phòng cho customer với các filter (giá, số người, tiện ích...)
        /// </summary>
        Task<ResultModel> SearchRoomTypesAsync(SearchRoomTypeRequest request);
        
        /// <summary>
        /// Lấy chi tiết loại phòng cho customer (public)
        /// </summary>
        Task<ResultModel> GetRoomTypeDetailForCustomerAsync(int roomTypeId, DateTime? checkInDate = null, DateTime? checkOutDate = null);
        
        // ============= ROOM TYPE STATISTICS & ANALYTICS =============
        /// <summary>
        /// Tìm kiếm và thống kê loại phòng với nhiều filter khác nhau
        /// Bao gồm: thống kê tổng quan, top được đặt nhiều nhất, filter theo giá/số khách/thời gian
        /// </summary>
        Task<ResultModel> SearchRoomTypeStatisticsAsync(RoomTypeStatisticsRequest request);
        
        // ============= ROOM TYPE CRUD (FOR ADMIN) =============
        Task<ResultModel> GetRoomTypeListAsync(GetRoomTypeListRequest request);
        Task<ResultModel> GetRoomTypeDetailAsync(int roomTypeId);
        Task<ResultModel> AddRoomTypeAsync(AddRoomTypeRequest request, int userId);
        Task<ResultModel> UpdateRoomTypeAsync(UpdateRoomTypeRequest request, int userId);
        Task<ResultModel> DeleteRoomTypeAsync(int roomTypeId, int userId);
        
        // ============= ROOM CRUD (FOR ADMIN ONLY) =============
        Task<ResultModel> GetRoomListAsync(GetRoomListRequest request);
        Task<ResultModel> GetRoomDetailAsync(int roomId);
        Task<ResultModel> AddRoomAsync(AddRoomRequest request, int userId);
        Task<ResultModel> UpdateRoomAsync(UpdateRoomRequest request, int userId);
        Task<ResultModel> DeleteRoomAsync(int roomId, int userId);
    }
}
