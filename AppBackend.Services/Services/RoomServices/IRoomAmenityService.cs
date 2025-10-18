using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.RoomModel;
using System.Threading.Tasks;

namespace AppBackend.Services.Services.RoomServices
{
    public interface IRoomAmenityService
    {
        Task<ResultModel> GetRoomAmenitiesAsync(int roomId);
        Task<ResultModel> GetRoomAmenitiesWithSelectionAsync(int roomId);
        Task<ResultModel> GetRoomsByAmenityAsync(int amenityId);
        Task<ResultModel> AddRoomAmenityAsync(AddRoomAmenityRequest request, int userId);
        Task<ResultModel> AddMultipleRoomAmenitiesAsync(AddMultipleRoomAmenitiesRequest request, int userId);
        Task<ResultModel> SyncRoomAmenitiesAsync(SyncRoomAmenitiesRequest request, int userId);
        Task<ResultModel> ToggleRoomAmenityAsync(AddRoomAmenityRequest request, int userId);
        Task<ResultModel> RemoveRoomAmenityAsync(RemoveRoomAmenityRequest request, int userId);
        Task<ResultModel> RemoveAllRoomAmenitiesAsync(int roomId, int userId);
    }
}
