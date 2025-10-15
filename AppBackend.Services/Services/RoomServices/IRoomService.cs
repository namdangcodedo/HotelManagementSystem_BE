using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.RoomModel;

namespace AppBackend.Services.Services.RoomServices
{
    public interface IRoomService
    {
        // Room operations
        Task<ResultModel> GetRoomListAsync(GetRoomListRequest request);
        Task<ResultModel> GetRoomDetailAsync(int roomId);
        Task<ResultModel> AddRoomAsync(AddRoomRequest request, int userId);
        Task<ResultModel> UpdateRoomAsync(UpdateRoomRequest request, int userId);
        Task<ResultModel> DeleteRoomAsync(int roomId, int userId);
        
        // Room Type operations
        Task<ResultModel> GetRoomTypeListAsync(GetRoomTypeListRequest request);
        Task<ResultModel> GetRoomTypeDetailAsync(int roomTypeId);
        Task<ResultModel> AddRoomTypeAsync(AddRoomTypeRequest request, int userId);
        Task<ResultModel> UpdateRoomTypeAsync(UpdateRoomTypeRequest request, int userId);
        Task<ResultModel> DeleteRoomTypeAsync(int roomTypeId, int userId);
    }
}

