using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.RoomModel;

namespace AppBackend.Services.Services.RoomServices
{
    public interface IRoomAmenityService
    {
        /// <summary>
        /// Lấy danh sách amenities của một phòng
        /// </summary>
        Task<ResultModel> GetRoomAmenitiesAsync(int roomId);
        
        /// <summary>
        /// Thêm một amenity vào phòng
        /// </summary>
        Task<ResultModel> AddRoomAmenityAsync(AddRoomAmenityRequest request, int userId);
        
        /// <summary>
        /// Xóa một amenity khỏi phòng
        /// </summary>
        Task<ResultModel> DeleteRoomAmenityAsync(int roomId, int amenityId);
        
        /// <summary>
        /// Cập nhật toàn bộ amenities cho một phòng (batch update)
        /// </summary>
        Task<ResultModel> UpdateRoomAmenitiesAsync(UpdateRoomAmenitiesRequest request, int userId);
    }
}
