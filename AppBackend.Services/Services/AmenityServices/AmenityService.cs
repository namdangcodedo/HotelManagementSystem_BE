using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.Services.AmenityServices
{
    public class AmenityService : IAmenityService
    {
        private readonly IUnitOfWork _unitOfWork;
        public AmenityService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<ResultModel> AddAmenityAsync(AmenityDto dto, int userId)
        {
            var amenity = new BusinessObjects.Models.Amenity
            {
                AmenityName = dto.AmenityName,
                Description = dto.Description,
                Price = dto.Price,
                CreatedAt = DateTime.UtcNow,
                IsActive = dto.IsActive,
                CreatedBy = userId
            };
            await _unitOfWork.Amenities.AddAsync(amenity);
            await _unitOfWork.SaveChangesAsync();

            // Add Medium images if provided
            if (dto.ImageLinks != null && dto.ImageLinks.Count > 0)
            {
                for (int i = 0; i < dto.ImageLinks.Count; i++)
                {
                    var medium = new Medium
                    {
                        FilePath = dto.ImageLinks[i],
                        ReferenceTable = nameof(BusinessObjects.Models.Amenity),
                        ReferenceKey = amenity.AmenityId.ToString(),
                        Description = $"Amenity image {i+1}",
                        DisplayOrder = i,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId,
                        UpdatedAt = null,
                        UpdatedBy = null
                    };
                    await _unitOfWork.Mediums.AddAsync(medium);
                }
                await _unitOfWork.SaveChangesAsync();
            }
            return new ResultModel { IsSuccess = true, Message = "Thêm tiện ích thành công." };
        }

        public async Task<ResultModel> UpdateAmenityAsync(AmenityDto dto, int userId)
        {
            var amenity = await _unitOfWork.Amenities.GetByIdAsync(dto.AmenityId);
            if (amenity == null)
                return new ResultModel { IsSuccess = false, Message = "Tiện ích không tồn tại." };
            amenity.AmenityName = dto.AmenityName;
            amenity.Description = dto.Description;
            amenity.Price = dto.Price;
            amenity.IsActive = dto.IsActive;
            amenity.UpdatedAt = DateTime.UtcNow;
            amenity.UpdatedBy = userId; 
            await _unitOfWork.Amenities.UpdateAsync(amenity);
            await _unitOfWork.SaveChangesAsync();

            // Remove old Medium images and add new ones if provided
            var oldMedia = await _unitOfWork.Mediums.FindAsync(m => m.ReferenceTable == nameof(BusinessObjects.Models.Amenity) && m.ReferenceKey == amenity.AmenityId.ToString());
            foreach (var medium in oldMedia)
            {
                await _unitOfWork.Mediums.DeleteAsync(medium);
            }
            if (dto.ImageLinks != null && dto.ImageLinks.Count > 0)
            {
                for (int i = 0; i < dto.ImageLinks.Count; i++)
                {
                    var medium = new Medium
                    {
                        FilePath = dto.ImageLinks[i],
                        ReferenceTable = nameof(BusinessObjects.Models.Amenity),
                        ReferenceKey = amenity.AmenityId.ToString(),
                        Description = $"Amenity image {i+1}",
                        DisplayOrder = i,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = amenity.CreatedBy,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = userId
                    };
                    await _unitOfWork.Mediums.AddAsync(medium);
                }
            }
            await _unitOfWork.SaveChangesAsync();
            return new ResultModel { IsSuccess = true, Message = "Cập nhật tiện ích thành công." };
        }
        public async Task<ResultModel> DeleteAmenityAsync(int id, int userId)
        {
            var amenity = await _unitOfWork.Amenities.GetByIdAsync(id);
            if (amenity == null)
                return new ResultModel { IsSuccess = false, Message = "Tiện ích không tồn tại." };
            amenity.IsActive = false;
            amenity.UpdatedAt = DateTime.UtcNow;
            amenity.UpdatedBy = userId;
            await _unitOfWork.Amenities.UpdateAsync(amenity);
            await _unitOfWork.SaveChangesAsync();
            return new ResultModel { IsSuccess = true, Message = "Gỡ tiện ích thành công." };
        }
        public async Task<ResultModel> GetAmenityListAsync(bool? isActive = null)
        {
            var list = await _unitOfWork.Amenities.GetAllAsync(isActive);
            return new ResultModel { IsSuccess = true, Message = "Lấy danh sách tiện ích thành công.", Data = list };
        }
        public async Task<ResultModel> GetAmenityPagedAsync(PagedAmenityRequestDto request)
        {
            var (items, totalCount) = await _unitOfWork.Amenities.GetPagedAsync(
                request.PageIndex,
                request.PageSize,
                request.IsActive,
                request.Search,
                request.SortBy,
                request.SortDesc
            );
            return new ResultModel
            {
                IsSuccess = true,
                Message = "Lấy danh sách tiện ích thành công.",
                Data = new
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageIndex = request.PageIndex,
                    PageSize = request.PageSize
                }
            };
        }
    }
}
