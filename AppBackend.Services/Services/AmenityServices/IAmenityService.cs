using AppBackend.BusinessObjects.Dtos;
using AppBackend.Services.ApiModels;

namespace AppBackend.Services.Services.AmenityServices
{
    public interface IAmenityService
    {
        Task<ResultModel> AddAmenityAsync(AmenityDto dto, int userId);
        Task<ResultModel> UpdateAmenityAsync(AmenityDto dto, int userId);
        Task<ResultModel> DeleteAmenityAsync(int id, int userId);
        Task<ResultModel> GetAmenityListAsync(bool? isActive = null, string? amenityType = null);
        Task<ResultModel> GetAllAmenitiesAsync(); // Lấy tất cả không filter (cho dropdown/selection)
        Task<ResultModel> GetAmenityPagedAsync(PagedAmenityRequestDto request);
        Task<ResultModel> GetAmenityDetailAsync(int id);
    }
}
