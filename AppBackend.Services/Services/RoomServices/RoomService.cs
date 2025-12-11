using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.BusinessObjects.Models;
using AppBackend.BusinessObjects.Enums;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.RoomModel;
using AppBackend.Services.Services.MediaService;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RoomTypeModel = AppBackend.BusinessObjects.Models.RoomType;

namespace AppBackend.Services.Services.RoomServices
{
    public class RoomService : IRoomService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<RoomService> _logger;
        private readonly IMediaService _mediaService;

        public RoomService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<RoomService> logger, IMediaService mediaService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _mediaService = mediaService;
        }

        #region ROOM TYPE SEARCH - FOR CUSTOMER

        /// <summary>
        /// Tìm kiếm loại phòng cho customer với các filter
        /// </summary>
        public async Task<ResultModel> SearchRoomTypesAsync(SearchRoomTypeRequest request)
        {
            _logger.LogInformation("=== SearchRoomTypesAsync CALLED ===");
            _logger.LogInformation("CheckInDate: {CheckIn}, CheckOutDate: {CheckOut}", request.CheckInDate, request.CheckOutDate);
            _logger.LogInformation("NumberOfGuests: {Guests}, MinPrice: {MinPrice}, MaxPrice: {MaxPrice}",
                request.NumberOfGuests, request.MinPrice, request.MaxPrice);
            _logger.LogInformation("OnlyActive: {OnlyActive}", request.OnlyActive);

            var query = _unitOfWork.RoomTypes.FindAsync(rt => true);
            var roomTypes = (await query).AsQueryable();

            // Chỉ hiển thị RoomType active
            if (request.OnlyActive)
            {
                roomTypes = roomTypes.Where(rt => rt.IsActive);
            }

            // Lọc theo số lượng khách
            if (request.NumberOfGuests.HasValue)
            {
                roomTypes = roomTypes.Where(rt => rt.MaxOccupancy >= request.NumberOfGuests.Value);
            }

            // Lọc theo giá
            if (request.MinPrice.HasValue)
            {
                roomTypes = roomTypes.Where(rt => rt.BasePriceNight >= request.MinPrice.Value);
            }
            if (request.MaxPrice.HasValue)
            {
                roomTypes = roomTypes.Where(rt => rt.BasePriceNight <= request.MaxPrice.Value);
            }

            // Lọc theo loại giường
            if (!string.IsNullOrWhiteSpace(request.BedType))
            {
                roomTypes = roomTypes.Where(rt => rt.BedType != null && rt.BedType.Contains(request.BedType));
            }

            // Lọc theo diện tích
            if (request.MinRoomSize.HasValue)
            {
                roomTypes = roomTypes.Where(rt => rt.RoomSize != null && rt.RoomSize >= request.MinRoomSize.Value);
            }

            // Sắp xếp theo giá (mặc định)
            roomTypes = roomTypes.OrderBy(rt => rt.BasePriceNight);

            var allRoomTypes = roomTypes.ToList();

            // Map sang DTO với images, amenities và availability
            var roomTypeResults = new List<RoomTypeSearchResultDto>();
            foreach (var rt in allRoomTypes)
            {
                // Lấy images
                var images = (await _unitOfWork.Mediums.FindAsync(m =>
                    m.ReferenceTable == "RoomType" && m.ReferenceKey == rt.RoomTypeId.ToString()))
                    .OrderBy(m => m.DisplayOrder)
                    .ToList();

                // Lấy tất cả phòng thuộc loại này
                var allRoomsOfType = (await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == rt.RoomTypeId)).ToList();
                var allCommentOfType = (await _unitOfWork.Comments.FindAsync(c => c.RoomTypeId == rt.RoomTypeId)).ToList();
                var totalRoomCount = allRoomsOfType.Count;
                
                // Tính số phòng available nếu có CheckIn/Out date
                int? availableRoomCount = null;
                if (request.CheckInDate.HasValue && request.CheckOutDate.HasValue)
                {
                    availableRoomCount = await CountAvailableRoomsAsync(rt.RoomTypeId, request.CheckInDate.Value, request.CheckOutDate.Value);
                }

                // Lấy amenities (có thể lấy từ một phòng mẫu hoặc định nghĩa riêng cho RoomType)
                // Tạm thời lấy amenities từ phòng đầu tiên của loại này
                var amenities = new List<AmenityDto>();
                var sampleRoom = allRoomsOfType.FirstOrDefault();
                if (sampleRoom != null)
                {
                    var roomAmenities = await _unitOfWork.RoomAmenities.GetAmenitiesByRoomIdAsync(sampleRoom.RoomId);
                    amenities = roomAmenities
                        .Where(ra => ra.Amenity.IsActive)
                        .Select(ra => new AmenityDto
                        {
                            AmenityId = ra.Amenity.AmenityId,
                            AmenityName = ra.Amenity.AmenityName,
                            Description = ra.Amenity.Description,
                            AmenityType = ra.Amenity.AmenityType,
                            IsActive = ra.Amenity.IsActive
                        }).ToList();
                }

                var comments = _mapper.Map<List<CommentDTO>>(allCommentOfType);

                var dto = new RoomTypeSearchResultDto
                {
                    RoomTypeId = rt.RoomTypeId,
                    TypeName = rt.TypeName,
                    TypeCode = rt.TypeCode,
                    Description = rt.Description,
                    BasePriceNight = rt.BasePriceNight,
                    MaxOccupancy = rt.MaxOccupancy,
                    RoomSize = rt.RoomSize,
                    NumberOfBeds = rt.NumberOfBeds,
                    BedType = rt.BedType,
                    IsActive = rt.IsActive,
                    Images = _mapper.Map<List<MediumDto>>(images),
                    Amenities = amenities,
                    TotalRoomCount = totalRoomCount,
                    AvailableRoomCount = availableRoomCount,
                    Comments = comments
                };

                roomTypeResults.Add(dto);
            }

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = roomTypeResults,
                StatusCode = StatusCodes.Status200OK
            };
        }


        public async Task<ResultModel> SearchRoomTypesWithRoomAsync(SearchRoomTypeRequest request)
        {
            _logger.LogInformation("=== SearchRoomTypesWithRoomAsync CALLED ===");
            _logger.LogInformation("CheckInDate: {CheckIn}, CheckOutDate: {CheckOut}", request.CheckInDate, request.CheckOutDate);
            _logger.LogInformation("NumberOfGuests: {Guests}, MinPrice: {MinPrice}, MaxPrice: {MaxPrice}",
                request.NumberOfGuests, request.MinPrice, request.MaxPrice);
            _logger.LogInformation("OnlyActive: {OnlyActive}", request.OnlyActive);

            var query = _unitOfWork.RoomTypes.FindAsync(rt => true);
            var roomTypes = (await query).AsQueryable();

            // Chỉ hiển thị RoomType active
            if (request.OnlyActive)
            {
                roomTypes = roomTypes.Where(rt => rt.IsActive);
            }

            // Lọc theo số lượng khách
            if (request.NumberOfGuests.HasValue)
            {
                roomTypes = roomTypes.Where(rt => rt.MaxOccupancy >= request.NumberOfGuests.Value);
            }

            // Lọc theo giá
            if (request.MinPrice.HasValue)
            {
                roomTypes = roomTypes.Where(rt => rt.BasePriceNight >= request.MinPrice.Value);
            }
            if (request.MaxPrice.HasValue)
            {
                roomTypes = roomTypes.Where(rt => rt.BasePriceNight <= request.MaxPrice.Value);
            }

            // Lọc theo loại giường
            if (!string.IsNullOrWhiteSpace(request.BedType))
            {
                roomTypes = roomTypes.Where(rt => rt.BedType != null && rt.BedType.Contains(request.BedType));
            }

            // Lọc theo diện tích
            if (request.MinRoomSize.HasValue)
            {
                roomTypes = roomTypes.Where(rt => rt.RoomSize != null && rt.RoomSize >= request.MinRoomSize.Value);
            }

            // Sắp xếp theo giá (mặc định)
            roomTypes = roomTypes.OrderBy(rt => rt.BasePriceNight);

            var allRoomTypes = roomTypes.ToList();

            // Map sang DTO với images, amenities và availability
            var roomTypeResults = new List<RoomTypeSearchResultDto>();
            foreach (var rt in allRoomTypes)
            {
                // Lấy images
                var images = (await _unitOfWork.Mediums.FindAsync(m =>
                    m.ReferenceTable == "RoomType" && m.ReferenceKey == rt.RoomTypeId.ToString()))
                    .OrderBy(m => m.DisplayOrder)
                    .ToList();

                // Lấy tất cả phòng thuộc loại này
                var allRoomsOfType = (await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == rt.RoomTypeId)).ToList();
                var totalRoomCount = allRoomsOfType.Count;

                // Tính số phòng available nếu có CheckIn/Out date
                int? availableRoomCount = null;
                if (request.CheckInDate.HasValue && request.CheckOutDate.HasValue)
                {
                    availableRoomCount = await CountAvailableRoomsAsync(rt.RoomTypeId, request.CheckInDate.Value, request.CheckOutDate.Value);
                }

                // Lấy amenities (có thể lấy từ một phòng mẫu hoặc định nghĩa riêng cho RoomType)
                // Tạm thời lấy amenities từ phòng đầu tiên của loại này
                var amenities = new List<AmenityDto>();
                var sampleRoom = allRoomsOfType.FirstOrDefault();
                if (sampleRoom != null)
                {
                    var roomAmenities = await _unitOfWork.RoomAmenities.GetAmenitiesByRoomIdAsync(sampleRoom.RoomId);
                    amenities = roomAmenities
                        .Where(ra => ra.Amenity.IsActive)
                        .Select(ra => new AmenityDto
                        {
                            AmenityId = ra.Amenity.AmenityId,
                            AmenityName = ra.Amenity.AmenityName,
                            Description = ra.Amenity.Description,
                            AmenityType = ra.Amenity.AmenityType,
                            IsActive = ra.Amenity.IsActive
                        }).ToList();
                }

                var dto = new RoomTypeSearchResultDto
                {
                    RoomTypeId = rt.RoomTypeId,
                    TypeName = rt.TypeName,
                    TypeCode = rt.TypeCode,
                    Description = rt.Description,
                    BasePriceNight = rt.BasePriceNight,
                    MaxOccupancy = rt.MaxOccupancy,
                    RoomSize = rt.RoomSize,
                    NumberOfBeds = rt.NumberOfBeds,
                    BedType = rt.BedType,
                    IsActive = rt.IsActive,
                    Images = _mapper.Map<List<MediumDto>>(images),
                    Amenities = amenities,
                    TotalRoomCount = totalRoomCount,
                    AvailableRoomCount = availableRoomCount
                };

                roomTypeResults.Add(dto);
            }

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = roomTypeResults,
                StatusCode = StatusCodes.Status200OK
            };
        }
        /// <summary>
        /// Lấy chi tiết loại phòng cho customer
        /// </summary>
        public async Task<ResultModel> GetRoomTypeDetailForCustomerAsync(int roomTypeId, DateTime? checkInDate = null, DateTime? checkOutDate = null)
        {
            var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(roomTypeId);
            if (roomType == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Loại phòng"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Lấy images
            var images = (await _unitOfWork.Mediums.FindAsync(m =>
                m.ReferenceTable == "RoomType" && m.ReferenceKey == roomTypeId.ToString()))
                .OrderBy(m => m.DisplayOrder)
                .ToList();

            // Lấy tất cả phòng thuộc loại này
            var allRoomsOfType = (await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == roomTypeId)).ToList();
            var totalRoomCount = allRoomsOfType.Count;
            
            // Tính số phòng available
            int? availableRoomCount = null;
            if (checkInDate.HasValue && checkOutDate.HasValue)
            {
                availableRoomCount = await CountAvailableRoomsAsync(roomTypeId, checkInDate.Value, checkOutDate.Value);
            }

            // Lấy amenities từ phòng mẫu
            var amenities = new List<AmenityDto>();
            var sampleRoom = allRoomsOfType.FirstOrDefault();
            if (sampleRoom != null)
            {
                var roomAmenities = await _unitOfWork.RoomAmenities.GetAmenitiesByRoomIdAsync(sampleRoom.RoomId);
                amenities = roomAmenities
                    .Where(ra => ra.Amenity.IsActive)
                    .Select(ra => new AmenityDto
                    {
                        AmenityId = ra.Amenity.AmenityId,
                        AmenityName = ra.Amenity.AmenityName,
                        Description = ra.Amenity.Description,
                        AmenityType = ra.Amenity.AmenityType,
                        IsActive = ra.Amenity.IsActive
                    }).ToList();
            }

            var dto = new RoomTypeSearchResultDto
            {
                RoomTypeId = roomType.RoomTypeId,
                TypeName = roomType.TypeName,
                TypeCode = roomType.TypeCode,
                Description = roomType.Description,
                BasePriceNight = roomType.BasePriceNight,
                MaxOccupancy = roomType.MaxOccupancy,
                RoomSize = roomType.RoomSize,
                NumberOfBeds = roomType.NumberOfBeds,
                BedType = roomType.BedType,
                IsActive = roomType.IsActive,
                Images = _mapper.Map<List<MediumDto>>(images),
                Amenities = amenities,
                TotalRoomCount = totalRoomCount,
                AvailableRoomCount = availableRoomCount
            };

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = dto,
                StatusCode = StatusCodes.Status200OK
            };
        }

        /// <summary>
        /// Đếm số phòng available theo RoomType và CheckIn/Out
        /// </summary>
        private async Task<int> CountAvailableRoomsAsync(int roomTypeId, DateTime checkInDate, DateTime checkOutDate)
        {
            var allRoomsOfType = await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == roomTypeId);
            int availableCount = 0;

            foreach (var room in allRoomsOfType)
            {
                // Kiểm tra xem phòng có bị book trong khoảng thời gian này không
                var existingBookings = await _unitOfWork.BookingRooms.FindAsync(br =>
                    br.RoomId == room.RoomId &&
                    br.Booking.CheckInDate < checkOutDate &&
                    br.Booking.CheckOutDate > checkInDate);

                if (!existingBookings.Any())
                {
                    availableCount++;
                }
            }

            return availableCount;
        }

        #endregion

        #region ROOM TYPE CRUD - FOR ADMIN

        public async Task<ResultModel> GetRoomTypeListAsync(GetRoomTypeListRequest request)
        {
            var query = _unitOfWork.RoomTypes.FindAsync(rt => true);
            var roomTypes = (await query).AsQueryable();

            // Lọc theo IsActive
            if (request.IsActive.HasValue)
            {
                roomTypes = roomTypes.Where(rt => rt.IsActive == request.IsActive.Value);
            }

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                roomTypes = roomTypes.Where(rt =>
                    rt.TypeName.Contains(request.Search) ||
                    rt.TypeCode.Contains(request.Search) ||
                    (rt.Description != null && rt.Description.Contains(request.Search)));
            }

            // Sắp xếp
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                roomTypes = request.SortDesc
                    ? roomTypes.OrderByDescending(rt => EF.Property<object>(rt, request.SortBy))
                    : roomTypes.OrderBy(rt => EF.Property<object>(rt, request.SortBy));
            }
            else
            {
                roomTypes = roomTypes.OrderBy(rt => rt.TypeName);
            }

            // Lấy tất cả (không phân trang vì số lượng phòng ít)
            var allRoomTypes = roomTypes.ToList();
            var totalRecords = allRoomTypes.Count;

            // Map với images và đếm số phòng
            var roomTypeDtos = new List<RoomTypeWithImagesDto>();
            foreach (var rt in allRoomTypes)
            {
                var images = (await _unitOfWork.Mediums.FindAsync(m =>
                    m.ReferenceTable == "RoomType" && m.ReferenceKey == rt.RoomTypeId.ToString()))
                    .OrderBy(m => m.DisplayOrder)
                    .ToList();

                var totalRooms = (await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == rt.RoomTypeId)).Count();

                var dto = new RoomTypeWithImagesDto
                {
                    RoomTypeId = rt.RoomTypeId,
                    TypeName = rt.TypeName,
                    TypeCode = rt.TypeCode,
                    Description = rt.Description,
                    BasePriceNight = rt.BasePriceNight,
                    MaxOccupancy = rt.MaxOccupancy,
                    RoomSize = rt.RoomSize,
                    NumberOfBeds = rt.NumberOfBeds,
                    BedType = rt.BedType,
                    IsActive = rt.IsActive,
                    Images = _mapper.Map<List<MediumDto>>(images),
                    TotalRooms = totalRooms,
                    CreatedAt = rt.CreatedAt,
                    UpdatedAt = rt.UpdatedAt
                };

                roomTypeDtos.Add(dto);
            }

            // Trả về tất cả items (không phân trang)
            var response = new PagedResponseDto<RoomTypeWithImagesDto>
            {
                Items = roomTypeDtos,
                TotalCount = totalRecords,
                PageIndex = 1,
                PageSize = totalRecords > 0 ? totalRecords : 1,
                TotalPages = 1
            };

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = response,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetRoomTypeDetailAsync(int roomTypeId)
        {
            var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(roomTypeId);
            if (roomType == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Loại phòng"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            var images = (await _unitOfWork.Mediums.FindAsync(m =>
                m.ReferenceTable == "RoomType" && m.ReferenceKey == roomTypeId.ToString()))
                .OrderBy(m => m.DisplayOrder)
                .ToList();

            var totalRooms = (await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == roomTypeId)).Count();

            var dto = new RoomTypeWithImagesDto
            {
                RoomTypeId = roomType.RoomTypeId,
                TypeName = roomType.TypeName,
                TypeCode = roomType.TypeCode,
                Description = roomType.Description,
                BasePriceNight = roomType.BasePriceNight,
                MaxOccupancy = roomType.MaxOccupancy,
                RoomSize = roomType.RoomSize,
                NumberOfBeds = roomType.NumberOfBeds,
                BedType = roomType.BedType,
                IsActive = roomType.IsActive,
                Images = _mapper.Map<List<MediumDto>>(images),
                TotalRooms = totalRooms,
                CreatedAt = roomType.CreatedAt,
                UpdatedAt = roomType.UpdatedAt
            };

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = dto,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> AddRoomTypeAsync(AddRoomTypeRequest request, int userId)
        {
            // Kiểm tra TypeCode đã tồn tại
            var existing = (await _unitOfWork.RoomTypes.FindAsync(rt => rt.TypeCode == request.TypeCode)).FirstOrDefault();
            if (existing != null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.EXISTED,
                    Message = string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Mã loại phòng"),
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Tạo RoomType mới
            var roomType = new RoomTypeModel
            {
                TypeName = request.TypeName,
                TypeCode = request.TypeCode,
                Description = request.Description,
                BasePriceNight = request.BasePriceNight,
                MaxOccupancy = request.MaxOccupancy,
                RoomSize = request.RoomSize,
                NumberOfBeds = request.NumberOfBeds,
                BedType = request.BedType,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            await _unitOfWork.RoomTypes.AddAsync(roomType);
            await _unitOfWork.SaveChangesAsync();

            // Thêm images nếu có
            if (request.ImageUrls != null && request.ImageUrls.Any())
            {
                int order = 0;
                foreach (var imageUrl in request.ImageUrls)
                {
                    var medium = new Medium
                    {
                        ReferenceKey = roomType.RoomTypeId.ToString(),
                        ReferenceTable = "RoomType",
                        FilePath = imageUrl,
                        Description = $"RoomType {request.TypeName} Image",
                        DisplayOrder = order++,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    };
                    await _unitOfWork.Mediums.AddAsync(medium);
                }
                await _unitOfWork.SaveChangesAsync();
            }

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Thêm loại phòng thành công",
                Data = new { roomType.RoomTypeId, roomType.TypeName },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> UpdateRoomTypeAsync(UpdateRoomTypeRequest request, int userId)
        {
            var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(request.RoomTypeId);
            if (roomType == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Loại phòng"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Kiểm tra TypeCode mới có trùng không
            if (!string.IsNullOrWhiteSpace(request.TypeCode) && request.TypeCode != roomType.TypeCode)
            {
                var existing = (await _unitOfWork.RoomTypes.FindAsync(rt => rt.TypeCode == request.TypeCode)).FirstOrDefault();
                if (existing != null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.EXISTED,
                        Message = string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Mã loại phòng"),
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
                roomType.TypeCode = request.TypeCode;
            }

            // Cập nhật các trường
            if (!string.IsNullOrWhiteSpace(request.TypeName)) roomType.TypeName = request.TypeName;
            if (request.Description != null) roomType.Description = request.Description;
            if (request.BasePriceNight.HasValue) roomType.BasePriceNight = request.BasePriceNight.Value;
            if (request.MaxOccupancy.HasValue) roomType.MaxOccupancy = request.MaxOccupancy.Value;
            if (request.RoomSize.HasValue) roomType.RoomSize = request.RoomSize.Value;
            if (request.NumberOfBeds.HasValue) roomType.NumberOfBeds = request.NumberOfBeds.Value;
            if (request.BedType != null) roomType.BedType = request.BedType;
            if (request.IsActive.HasValue) roomType.IsActive = request.IsActive.Value;

            roomType.UpdatedAt = DateTime.UtcNow;
            roomType.UpdatedBy = userId;

            await _unitOfWork.RoomTypes.UpdateAsync(roomType);
            await _unitOfWork.SaveChangesAsync();

            // Cập nhật images nếu có (using new media CRUD system)
            if (request.ImageMedia != null || request.ImageUrls != null)
            {
                // Support both new ImageMedia and legacy ImageUrls for backward compatibility
                if (request.ImageMedia != null)
                {
                    // Use new CRUD media system
                    await _mediaService.ProcessMediaCrudAsync(
                        request.ImageMedia,
                        "RoomType",
                        roomType.RoomTypeId,
                        userId);
                }
                else if (request.ImageUrls != null)
                {
                    // Legacy support: convert ImageUrls to add actions
                    var mediaCrudItems = request.ImageUrls
                        .Select(url => new AppBackend.Services.ApiModels.Commons.MediaCrudDto
                        {
                            CrudKey = "add",
                            Url = url,
                            AltText = $"RoomType {roomType.TypeName} Image"
                        })
                        .ToList();

                    await _mediaService.ProcessMediaCrudAsync(
                        mediaCrudItems,
                        "RoomType",
                        roomType.RoomTypeId,
                        userId);
                }
            }

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Cập nhật loại phòng thành công",
                Data = new { roomType.RoomTypeId, roomType.TypeName },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> DeleteRoomTypeAsync(int roomTypeId, int userId)
        {
            var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(roomTypeId);
            if (roomType == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Loại phòng"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Kiểm tra có phòng nào thuộc loại này không
            var rooms = await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == roomTypeId);
            if (rooms.Any())
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.FAILED,
                    Message = "Không thể xóa loại phòng vì còn phòng thuộc loại này",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Xóa images liên quan
            var images = await _unitOfWork.Mediums.FindAsync(m =>
                m.ReferenceTable == "RoomType" && m.ReferenceKey == roomTypeId.ToString());
            foreach (var img in images)
            {
                await _unitOfWork.Mediums.DeleteAsync(img);
            }

            await _unitOfWork.RoomTypes.DeleteAsync(roomType);
            await _unitOfWork.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Xóa loại phòng thành công",
                StatusCode = StatusCodes.Status200OK
            };
        }

        #endregion

        #region ROOM CRUD - FOR ADMIN ONLY

        public async Task<ResultModel> GetRoomListAsync(GetRoomListRequest request)
        {
            var query = _unitOfWork.Rooms.FindAsync(r => true);
            var rooms = (await query).AsQueryable();

            // Lọc theo RoomType
            if (request.RoomTypeId.HasValue)
            {
                rooms = rooms.Where(r => r.RoomTypeId == request.RoomTypeId.Value);
            }

            // Lọc theo Status
            if (request.StatusId.HasValue)
            {
                rooms = rooms.Where(r => r.StatusId == request.StatusId.Value);
            }

            // Tìm kiếm theo tên phòng
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                rooms = rooms.Where(r =>
                    r.RoomName.Contains(request.Search) ||
                    (r.Description != null && r.Description.Contains(request.Search)));
            }

            // Sắp xếp
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                rooms = request.SortDesc
                    ? rooms.OrderByDescending(r => EF.Property<object>(r, request.SortBy))
                    : rooms.OrderBy(r => EF.Property<object>(r, request.SortBy));
            }
            else
            {
                rooms = rooms.OrderBy(r => r.RoomName);
            }

            // Tổng số bản ghi
            var totalRecords = rooms.Count();

            // Phân trang
            var pagedRooms = rooms
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Map sang DTO với images và amenities
            var roomDtos = new List<RoomWithImagesDto>();
            foreach (var room in pagedRooms)
            {
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
                var status = await _unitOfWork.CommonCodes.GetByIdAsync(room.StatusId);
                
                var images = (await _unitOfWork.Mediums.FindAsync(m =>
                    m.ReferenceTable == "Room" && m.ReferenceKey == room.RoomId.ToString()))
                    .OrderBy(m => m.DisplayOrder)
                    .ToList();

                // Lấy amenities của phòng
                var roomAmenities = await _unitOfWork.RoomAmenities.GetAmenitiesByRoomIdAsync(room.RoomId);
                var amenities = roomAmenities
                    .Where(ra => ra.Amenity.IsActive)
                    .Select(ra => new AmenityDto
                    {
                        AmenityId = ra.Amenity.AmenityId,
                        AmenityName = ra.Amenity.AmenityName,
                        Description = ra.Amenity.Description,
                        AmenityType = ra.Amenity.AmenityType,
                        IsActive = ra.Amenity.IsActive
                    }).ToList();

                var roomDto = new RoomWithImagesDto
                {
                    RoomId = room.RoomId,
                    RoomName = room.RoomName,
                    RoomTypeId = room.RoomTypeId,
                    RoomTypeName = roomType?.TypeName,
                    StatusId = room.StatusId,
                    StatusName = status?.CodeValue,
                    Description = room.Description,
                    Images = _mapper.Map<List<MediumDto>>(images),
                    Amenities = amenities,
                    CreatedAt = room.CreatedAt,
                    UpdatedAt = room.UpdatedAt
                };

                roomDtos.Add(roomDto);
            }

            var pagedResponse = new PagedResponseDto<RoomWithImagesDto>
            {
                Items = roomDtos,
                TotalCount = totalRecords,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize)
            };

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = pagedResponse,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetRoomDetailAsync(int roomId)
        {
            var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
            if (room == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Phòng"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
            var status = await _unitOfWork.CommonCodes.GetByIdAsync(room.StatusId);
            
            var images = (await _unitOfWork.Mediums.FindAsync(m =>
                m.ReferenceTable == "Room" && m.ReferenceKey == roomId.ToString()))
                .OrderBy(m => m.DisplayOrder)
                .ToList();

            // Lấy amenities của phòng
            var roomAmenities = await _unitOfWork.RoomAmenities.GetAmenitiesByRoomIdAsync(roomId);
            var amenities = roomAmenities
                .Where(ra => ra.Amenity.IsActive)
                .Select(ra => new AmenityDto
                {
                    AmenityId = ra.Amenity.AmenityId,
                    AmenityName = ra.Amenity.AmenityName,
                    Description = ra.Amenity.Description,
                    AmenityType = ra.Amenity.AmenityType,
                    IsActive = ra.Amenity.IsActive
                }).ToList();

            var roomDto = new RoomWithImagesDto
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                RoomTypeId = room.RoomTypeId,
                RoomTypeName = roomType?.TypeName,
                StatusId = room.StatusId,
                StatusName = status?.CodeValue,
                Description = room.Description,
                Images = _mapper.Map<List<MediumDto>>(images),
                Amenities = amenities,
                CreatedAt = room.CreatedAt,
                UpdatedAt = room.UpdatedAt
            };

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = roomDto,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> AddRoomAsync(AddRoomRequest request, int userId)
        {
            // Kiểm tra tên phòng đã tồn tại
            var existingRoom = (await _unitOfWork.Rooms.FindAsync(r => r.RoomName == request.RoomName)).FirstOrDefault();
            if (existingRoom != null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.EXISTED,
                    Message = string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Tên phòng"),
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Kiểm tra RoomType tồn tại
            var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(request.RoomTypeId);
            if (roomType == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Loại phòng"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Tạo Room mới
            var room = new Room
            {
                RoomName = request.RoomName,
                RoomTypeId = request.RoomTypeId,
                StatusId = request.StatusId,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            await _unitOfWork.Rooms.AddAsync(room);
            await _unitOfWork.SaveChangesAsync();

            // Thêm images nếu có
            if (request.ImageMedia != null || (request.ImageUrls != null && request.ImageUrls.Any()))
            {
                // Preferred: use ImageMedia (client uploads to Cloudinary first and sends providerId/url + crudKey)
                if (request.ImageMedia != null)
                {
                    await _mediaService.ProcessMediaCrudAsync(request.ImageMedia, "Room", room.RoomId, userId);
                }
                else
                {
                    // Legacy: convert ImageUrls into add actions
                    var mediaCrudItems = request.ImageUrls.Select(url => new AppBackend.Services.ApiModels.Commons.MediaCrudDto
                    {
                        CrudKey = "add",
                        Url = url,
                        AltText = $"Room {room.RoomName} Image"
                    }).ToList();

                    await _mediaService.ProcessMediaCrudAsync(mediaCrudItems, "Room", room.RoomId, userId);
                }
            }

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Thêm phòng thành công",
                Data = new { room.RoomId, room.RoomName },
                StatusCode = StatusCodes.Status201Created
            };
        }

        public async Task<ResultModel> UpdateRoomAsync(UpdateRoomRequest request, int userId)
        {
            var room = await _unitOfWork.Rooms.GetByIdAsync(request.RoomId);
            if (room == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Phòng"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Kiểm tra tên phòng mới có trùng không
            if (!string.IsNullOrWhiteSpace(request.RoomName) && request.RoomName != room.RoomName)
            {
                var existingRoom = (await _unitOfWork.Rooms.FindAsync(r => r.RoomName == request.RoomName)).FirstOrDefault();
                if (existingRoom != null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.EXISTED,
                        Message = string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Tên phòng"),
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
                room.RoomName = request.RoomName;
            }

            // Cập nhật các trường
            if (request.RoomTypeId.HasValue) room.RoomTypeId = request.RoomTypeId.Value;
            if (request.StatusId.HasValue) room.StatusId = request.StatusId.Value;
            if (request.Description != null) room.Description = request.Description;

            room.UpdatedAt = DateTime.UtcNow;
            room.UpdatedBy = userId;

            await _unitOfWork.Rooms.UpdateAsync(room);
            await _unitOfWork.SaveChangesAsync();

            // Cập nhật images nếu có (using new media CRUD system)
            if (request.ImageMedia != null || request.ImageUrls != null)
            {
                // Support both new ImageMedia and legacy ImageUrls for backward compatibility
                if (request.ImageMedia != null)
                {
                    // Use new CRUD media system
                    await _mediaService.ProcessMediaCrudAsync(
                        request.ImageMedia,
                        "Room",
                        room.RoomId,
                        userId);
                }
                else if (request.ImageUrls != null)
                {
                    // Legacy support: convert ImageUrls to add actions
                    var mediaCrudItems = request.ImageUrls
                        .Select(url => new AppBackend.Services.ApiModels.Commons.MediaCrudDto
                        {
                            CrudKey = "add",
                            Url = url,
                            AltText = $"Room {room.RoomName} Image"
                        })
                        .ToList();

                    await _mediaService.ProcessMediaCrudAsync(
                        mediaCrudItems,
                        "Room",
                        room.RoomId,
                        userId);
                }
            }

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Cập nhật phòng thành công",
                Data = new { room.RoomId, room.RoomName },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> DeleteRoomAsync(int roomId, int userId)
        {
            var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
            if (room == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Phòng"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Kiểm tra xem phòng có booking nào không
            var bookings = await _unitOfWork.BookingRooms.FindAsync(br => br.RoomId == roomId);
            if (bookings.Any())
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.FAILED,
                    Message = "Không thể xóa phòng vì đã có booking",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Xóa images liên quan
            var images = await _unitOfWork.Mediums.FindAsync(m =>
                m.ReferenceTable == "Room" && m.ReferenceKey == roomId.ToString());
            foreach (var img in images)
            {
                await _unitOfWork.Mediums.DeleteAsync(img);
            }

            // Xóa RoomAmenities liên quan
            var roomAmenities = await _unitOfWork.RoomAmenities.GetAmenitiesByRoomIdAsync(roomId);
            foreach (var ra in roomAmenities)
            {
                await _unitOfWork.RoomAmenities.DeleteAsync(ra);
            }

            await _unitOfWork.Rooms.DeleteAsync(room);
            await _unitOfWork.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Xóa phòng thành công",
                StatusCode = StatusCodes.Status200OK
            };
        }

        #endregion

        #region ROOM TYPE STATISTICS & ANALYTICS

        /// <summary>
        /// Tìm kiếm và thống kê loại phòng với nhiều filter khác nhau
        /// </summary>
        public async Task<ResultModel> SearchRoomTypeStatisticsAsync(RoomTypeStatisticsRequest request)
        {
            _logger.LogInformation("SearchRoomTypeStatisticsAsync - Type: {Type}", request.StatisticType);

            return request.StatisticType.ToLower() switch
            {
                "overview" => await GetOverviewStatisticsAsync(request),
                "most_booked" => await GetMostBookedRoomTypesAsync(request),
                "by_price" => await GetRoomTypesByPriceAsync(request),
                "by_occupancy" => await GetRoomTypesByOccupancyAsync(request),
                "booking_stats" => await GetBookingStatisticsAsync(request),
                _ => new ResultModel
                {
                    IsSuccess = false,
                    Message = $"Loại thống kê '{request.StatisticType}' không hợp lệ. Sử dụng: overview, most_booked, by_price, by_occupancy, booking_stats",
                    StatusCode = StatusCodes.Status400BadRequest
                }
            };
        }

        private async Task<ResultModel> GetOverviewStatisticsAsync(RoomTypeStatisticsRequest request)
        {
            var allRoomTypes = await _unitOfWork.RoomTypes.GetAllAsync();
            
            if (request.OnlyActive)
            {
                allRoomTypes = allRoomTypes.Where(rt => rt.IsActive);
            }

            var statistics = new
            {
                TotalRoomTypes = allRoomTypes.Count(),
                ActiveRoomTypes = allRoomTypes.Count(rt => rt.IsActive),
                InactiveRoomTypes = allRoomTypes.Count(rt => !rt.IsActive),
                PriceRanges = new
                {
                    Budget = new { 
                        Count = allRoomTypes.Count(rt => rt.BasePriceNight < 500000),
                        Label = "< 500k VND"
                    },
                    Standard = new { 
                        Count = allRoomTypes.Count(rt => rt.BasePriceNight >= 500000 && rt.BasePriceNight < 1000000),
                        Label = "500k - 1M VND"
                    },
                    Deluxe = new { 
                        Count = allRoomTypes.Count(rt => rt.BasePriceNight >= 1000000 && rt.BasePriceNight < 2000000),
                        Label = "1M - 2M VND"
                    },
                    Luxury = new { 
                        Count = allRoomTypes.Count(rt => rt.BasePriceNight >= 2000000),
                        Label = ">= 2M VND"
                    }
                },
                OccupancyDistribution = allRoomTypes
                    .GroupBy(rt => rt.MaxOccupancy)
                    .Select(g => new { 
                        MaxGuests = g.Key, 
                        Count = g.Count(),
                        RoomTypes = g.Select(rt => new { rt.RoomTypeId, rt.TypeName, rt.BasePriceNight }).ToList()
                    })
                    .OrderBy(x => x.MaxGuests)
                    .ToList(),
                AveragePricePerNight = allRoomTypes.Any() ? allRoomTypes.Average(rt => rt.BasePriceNight) : 0,
                MinPrice = allRoomTypes.Any() ? allRoomTypes.Min(rt => rt.BasePriceNight) : 0,
                MaxPrice = allRoomTypes.Any() ? allRoomTypes.Max(rt => rt.BasePriceNight) : 0
            };

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Lấy thống kê tổng quan loại phòng thành công",
                Data = statistics,
                StatusCode = StatusCodes.Status200OK
            };
        }

        private async Task<ResultModel> GetMostBookedRoomTypesAsync(RoomTypeStatisticsRequest request)
        {
            var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => true);
            var rooms = await _unitOfWork.Rooms.GetAllAsync();
            var roomTypes = await _unitOfWork.RoomTypes.GetAllAsync();

            if (request.OnlyActive)
            {
                roomTypes = roomTypes.Where(rt => rt.IsActive);
            }

            // Filter by date if provided
            if (request.FromDate.HasValue || request.ToDate.HasValue)
            {
                bookingRooms = bookingRooms.Where(br =>
                {
                    if (request.FromDate.HasValue && br.CheckInDate < request.FromDate.Value)
                        return false;
                    if (request.ToDate.HasValue && br.CheckOutDate > request.ToDate.Value)
                        return false;
                    return true;
                });
            }

            var roomTypeStats = bookingRooms
                .Join(rooms, br => br.RoomId, r => r.RoomId, (br, r) => new { br, r })
                .Where(x => roomTypes.Any(rt => rt.RoomTypeId == x.r.RoomTypeId))
                .GroupBy(x => x.r.RoomTypeId)
                .Select(g => new
                {
                    RoomTypeId = g.Key,
                    BookingCount = g.Count(),
                    TotalRevenue = g.Sum(x => x.br.SubTotal),
                    TotalNights = g.Sum(x => x.br.NumberOfNights),
                    AveragePricePerNight = g.Average(x => x.br.PricePerNight)
                })
                .OrderByDescending(x => x.BookingCount)
                .Take(request.TopCount)
                .ToList();

            var result = roomTypeStats.Select(stat =>
            {
                var roomType = roomTypes.FirstOrDefault(rt => rt.RoomTypeId == stat.RoomTypeId);
                return new
                {
                    RoomTypeId = stat.RoomTypeId,
                    TypeName = roomType?.TypeName ?? "N/A",
                    TypeCode = roomType?.TypeCode ?? "N/A",
                    CurrentBasePriceNight = roomType?.BasePriceNight ?? 0,
                    MaxOccupancy = roomType?.MaxOccupancy ?? 0,
                    BookingCount = stat.BookingCount,
                    TotalRevenue = stat.TotalRevenue,
                    TotalNights = stat.TotalNights,
                    AveragePricePerNight = stat.AveragePricePerNight,
                    AverageRevenuePerBooking = stat.BookingCount > 0 ? stat.TotalRevenue / stat.BookingCount : 0
                };
            }).ToList();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Lấy top {request.TopCount} loại phòng được đặt nhiều nhất thành công",
                Data = new
                {
                    TopCount = request.TopCount,
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    RoomTypes = result
                },
                StatusCode = StatusCodes.Status200OK
            };
        }

        private async Task<ResultModel> GetRoomTypesByPriceAsync(RoomTypeStatisticsRequest request)
        {
            var roomTypes = await _unitOfWork.RoomTypes.GetAllAsync();
            
            if (request.OnlyActive)
            {
                roomTypes = roomTypes.Where(rt => rt.IsActive);
            }

            if (request.MinPrice.HasValue)
            {
                roomTypes = roomTypes.Where(rt => rt.BasePriceNight >= request.MinPrice.Value);
            }
            if (request.MaxPrice.HasValue)
            {
                roomTypes = roomTypes.Where(rt => rt.BasePriceNight <= request.MaxPrice.Value);
            }

            var result = roomTypes
                .OrderBy(rt => rt.BasePriceNight)
                .Select(rt => new
                {
                    RoomTypeId = rt.RoomTypeId,
                    TypeName = rt.TypeName,
                    TypeCode = rt.TypeCode,
                    Description = rt.Description,
                    BasePriceNight = rt.BasePriceNight,
                    MaxOccupancy = rt.MaxOccupancy,
                    RoomSize = rt.RoomSize,
                    NumberOfBeds = rt.NumberOfBeds,
                    BedType = rt.BedType,
                    IsActive = rt.IsActive
                })
                .ToList();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Tìm thấy {result.Count} loại phòng trong khoảng giá {request.MinPrice ?? 0:N0} - {request.MaxPrice?.ToString("N0") ?? "∞"} VND",
                Data = new
                {
                    MinPrice = request.MinPrice,
                    MaxPrice = request.MaxPrice,
                    TotalCount = result.Count,
                    RoomTypes = result
                },
                StatusCode = StatusCodes.Status200OK
            };
        }

        private async Task<ResultModel> GetRoomTypesByOccupancyAsync(RoomTypeStatisticsRequest request)
        {
            var roomTypes = await _unitOfWork.RoomTypes.GetAllAsync();
            
            if (request.OnlyActive)
            {
                roomTypes = roomTypes.Where(rt => rt.IsActive);
            }

            if (request.MinOccupancy.HasValue)
            {
                roomTypes = roomTypes.Where(rt => rt.MaxOccupancy >= request.MinOccupancy.Value);
            }
            if (request.MaxOccupancy.HasValue)
            {
                roomTypes = roomTypes.Where(rt => rt.MaxOccupancy <= request.MaxOccupancy.Value);
            }

            var result = roomTypes
                .OrderBy(rt => rt.MaxOccupancy)
                .ThenBy(rt => rt.BasePriceNight)
                .Select(rt => new
                {
                    RoomTypeId = rt.RoomTypeId,
                    TypeName = rt.TypeName,
                    TypeCode = rt.TypeCode,
                    Description = rt.Description,
                    BasePriceNight = rt.BasePriceNight,
                    MaxOccupancy = rt.MaxOccupancy,
                    RoomSize = rt.RoomSize,
                    NumberOfBeds = rt.NumberOfBeds,
                    BedType = rt.BedType,
                    IsActive = rt.IsActive
                })
                .ToList();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Tìm thấy {result.Count} loại phòng cho {request.MinOccupancy ?? 0}-{request.MaxOccupancy?.ToString() ?? "∞"} khách",
                Data = new
                {
                    MinOccupancy = request.MinOccupancy,
                    MaxOccupancy = request.MaxOccupancy,
                    TotalCount = result.Count,
                    RoomTypes = result
                },
                StatusCode = StatusCodes.Status200OK
            };
        }

        private async Task<ResultModel> GetBookingStatisticsAsync(RoomTypeStatisticsRequest request)
        {
            var bookings = await _unitOfWork.Bookings.FindAsync(b => true);
            var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => true);
            var rooms = await _unitOfWork.Rooms.GetAllAsync();
            var roomTypes = await _unitOfWork.RoomTypes.GetAllAsync();

            if (request.OnlyActive)
            {
                roomTypes = roomTypes.Where(rt => rt.IsActive);
            }

            // Filter bookings by date
            if (request.FromDate.HasValue)
            {
                bookings = bookings.Where(b => b.CheckInDate >= request.FromDate.Value);
            }
            if (request.ToDate.HasValue)
            {
                bookings = bookings.Where(b => b.CheckOutDate <= request.ToDate.Value);
            }

            var validBookingIds = bookings.Select(b => b.BookingId).ToList();

            var stats = bookingRooms
                .Where(br => validBookingIds.Contains(br.BookingId))
                .Join(rooms, br => br.RoomId, r => r.RoomId, (br, r) => new { br, r })
                .Where(x => roomTypes.Any(rt => rt.RoomTypeId == x.r.RoomTypeId))
                .GroupBy(x => x.r.RoomTypeId)
                .Select(g => new
                {
                    RoomTypeId = g.Key,
                    TotalBookings = g.Count(),
                    TotalRevenue = g.Sum(x => x.br.SubTotal),
                    TotalNights = g.Sum(x => x.br.NumberOfNights),
                    AveragePrice = g.Average(x => x.br.PricePerNight),
                    MinPrice = g.Min(x => x.br.PricePerNight),
                    MaxPrice = g.Max(x => x.br.PricePerNight)
                })
                .ToList();

            var result = stats.Select(stat =>
            {
                var roomType = roomTypes.FirstOrDefault(rt => rt.RoomTypeId == stat.RoomTypeId);
                return new
                {
                    RoomTypeId = stat.RoomTypeId,
                    TypeName = roomType?.TypeName ?? "N/A",
                    TypeCode = roomType?.TypeCode ?? "N/A",
                    CurrentBasePriceNight = roomType?.BasePriceNight ?? 0,
                    MaxOccupancy = roomType?.MaxOccupancy ?? 0,
                    Statistics = new
                    {
                        TotalBookings = stat.TotalBookings,
                        TotalRevenue = stat.TotalRevenue,
                        TotalNights = stat.TotalNights,
                        AveragePricePerNight = stat.AveragePrice,
                        MinPricePerNight = stat.MinPrice,
                        MaxPricePerNight = stat.MaxPrice,
                        AverageRevenuePerBooking = stat.TotalBookings > 0 ? stat.TotalRevenue / stat.TotalBookings : 0
                    }
                };
            })
            .OrderByDescending(x => x.Statistics.TotalBookings)
            .ToList();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Thống kê booking của {result.Count} loại phòng từ {request.FromDate?.ToString("yyyy-MM-dd") ?? "∞"} đến {request.ToDate?.ToString("yyyy-MM-dd") ?? "∞"}",
                Data = new
                {
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    TotalRoomTypes = result.Count,
                    TotalBookings = result.Sum(x => x.Statistics.TotalBookings),
                    TotalRevenue = result.Sum(x => x.Statistics.TotalRevenue),
                    RoomTypes = result
                },
                StatusCode = StatusCodes.Status200OK
            };
        }

        #endregion
    }
}
