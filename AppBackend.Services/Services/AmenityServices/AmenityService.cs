using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.BusinessObjects.Dtos;
using AutoMapper;

namespace AppBackend.Services.Services.AmenityServices
{
    public class AmenityService : IAmenityService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public AmenityService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<ResultModel> AddAmenityAsync(AmenityDto dto, int userId)
        {
            var amenity = new BusinessObjects.Models.Amenity
            {
                AmenityName = dto.AmenityName,
                Description = dto.Description,
                AmenityType = dto.AmenityType,
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
            amenity.AmenityType = dto.AmenityType;
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
        public async Task<ResultModel> GetAmenityListAsync(bool? isActive = null, string? amenityType = null)
        {
            var list = await _unitOfWork.Amenities.GetAllAsync(isActive);
            
            // Lọc theo AmenityType nếu có
            if (!string.IsNullOrWhiteSpace(amenityType))
            {
                list = list.Where(a => a.AmenityType.Equals(amenityType, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            var amenityWithImagesList = new List<AmenityWithMediumDto>();
            foreach (var amenity in list)
            {
                var mediumList = await _unitOfWork.Mediums.FindAsync(m => m.ReferenceTable == "Amenity" && m.ReferenceKey == amenity.AmenityId.ToString());
                var imageLinks = mediumList.Select(m => m.FilePath).ToList();
                var amenityDto = _mapper.Map<AmenityWithMediumDto>(amenity);
                amenityDto.Images = imageLinks;
                amenityWithImagesList.Add(amenityDto);
            }
            return new ResultModel { IsSuccess = true, Message = "Lấy danh sách tiện ích thành công.", Data = amenityWithImagesList };
        }
        public async Task<ResultModel> GetAmenityPagedAsync(PagedAmenityRequestDto request)
        {
            var (items, totalCount) = await _unitOfWork.Amenities.GetPagedAsync(
                request.PageIndex,
                request.PageSize,
                request.IsActive,
                request.Search,
                request.SortBy,
                request.SortDesc,
                request.AmenityType
            );

            var amenityWithMediumList = new List<AmenityWithMediumDto>();
            foreach (var amenity in items)
            {
                var mediumList = await _unitOfWork.Mediums.FindAsync(m => m.ReferenceTable == "Amenity" && m.ReferenceKey == amenity.AmenityId.ToString());
                var imageLinks = mediumList.Select(m => m.FilePath).ToList();
                var amenityDto = _mapper.Map<AmenityWithMediumDto>(amenity);
                amenityDto.Images = imageLinks;
                amenityWithMediumList.Add(amenityDto);
            }
            return new ResultModel
            {
                IsSuccess = true,
                Message = "Lấy danh sách tiện ích thành công.",
                Data = new
                {
                    Items = amenityWithMediumList,
                    TotalCount = totalCount,
                    PageIndex = request.PageIndex,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
                }
            };
        }
        
        public async Task<ResultModel> GetAllAmenitiesAsync()
        {
            var list = await _unitOfWork.Amenities.GetAllAsync(isActive: true);
            var amenityDtoList = new List<AmenityDto>();
            
            foreach (var amenity in list)
            {
                var mediumList = await _unitOfWork.Mediums.FindAsync(m => m.ReferenceTable == "Amenity" && m.ReferenceKey == amenity.AmenityId.ToString());
                var imageLinks = mediumList.Select(m => m.FilePath).ToList();
                
                var dto = new AmenityDto
                {
                    AmenityId = amenity.AmenityId,
                    AmenityName = amenity.AmenityName,
                    Description = amenity.Description,
                    AmenityType = amenity.AmenityType,
                    IsActive = amenity.IsActive,
                    ImageLinks = imageLinks
                };
                amenityDtoList.Add(dto);
            }
            
            return new ResultModel 
            { 
                IsSuccess = true, 
                Message = "Lấy danh sách tiện ích thành công.", 
                Data = amenityDtoList 
            };
        }
        public async Task<ResultModel> GetAmenityDetailAsync(int id)
        {
            var amenity = await _unitOfWork.Amenities.GetByIdAsync(id);
            if (amenity == null)
                return new ResultModel { IsSuccess = false, Message = "Tiện ích không tồn tại." };
            var mediumList = await _unitOfWork.Mediums.FindAsync(m => m.ReferenceTable == "Amenity" && m.ReferenceKey == amenity.AmenityId.ToString());
            var imageLinks = mediumList.Select(m => m.FilePath).ToList();
            var amenityDto = _mapper.Map<AmenityWithMediumDto>(amenity);
            amenityDto.Images = imageLinks;
            return new ResultModel { IsSuccess = true, Message = "Lấy chi tiết tiện ích thành công.", Data = amenityDto };
        }
    }
}
