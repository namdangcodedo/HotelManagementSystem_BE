using AppBackend.BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.Services.ApiModels;

namespace AppBackend.Services.Services.AmenityServices
{
    public interface IAmenityService
    {
        Task<ResultModel> AddAmenityAsync(AmenityDto dto, int userId);
        Task<ResultModel> UpdateAmenityAsync(AmenityDto dto, int userId);
        Task<ResultModel> DeleteAmenityAsync(int id, int userId);
        Task<ResultModel> GetAmenityListAsync(bool? isActive = null);
        Task<ResultModel> GetAmenityPagedAsync(PagedAmenityRequestDto request);
    }
}
