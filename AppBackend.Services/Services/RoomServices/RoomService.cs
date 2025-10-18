using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.BusinessObjects.Models;
using AppBackend.BusinessObjects.Enums;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.RoomModel;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Services.Services.RoomServices
{
    public class RoomService : IRoomService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RoomService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        #region Room Operations

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

            // Lọc theo giá
            if (request.MinPrice.HasValue)
            {
                rooms = rooms.Where(r => r.BasePriceNight >= request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                rooms = rooms.Where(r => r.BasePriceNight <= request.MaxPrice.Value);
            }

            // Tìm kiếm theo số phòng hoặc mô tả
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                rooms = rooms.Where(r =>
                    r.RoomNumber.Contains(request.Search) ||
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
                rooms = rooms.OrderBy(r => r.RoomNumber);
            }

            // Tổng số bản ghi
            var totalRecords = rooms.Count();

            // Phân trang
            var pagedRooms = rooms
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Lấy tất cả Common amenities một lần duy nhất (tối ưu performance)
            var allCommonAmenities = (await _unitOfWork.Amenities.GetByTypeAsync(nameof(AmenityType.Common), isActive: true))
                .Select(a => new AmenityDto
                {
                    AmenityId = a.AmenityId,
                    AmenityName = a.AmenityName,
                    Description = a.Description,
                    AmenityType = a.AmenityType,
                    IsActive = a.IsActive
                }).ToList();

            // Map sang DTO với images và amenities
            var roomDtos = new List<RoomWithImagesDto>();
            foreach (var room in pagedRooms)
            {
                var roomType = await _unitOfWork.CommonCodes.GetByIdAsync(room.RoomTypeId);
                var status = await _unitOfWork.CommonCodes.GetByIdAsync(room.StatusId);
                
                var images = (await _unitOfWork.Mediums.FindAsync(m =>
                    m.ReferenceTable == "Room" && m.ReferenceKey == room.RoomId.ToString()))
                    .OrderBy(m => m.DisplayOrder)
                    .ToList();

                // Lấy Additional amenities đã được add vào room này
                var roomAmenities = await _unitOfWork.RoomAmenities.GetAmenitiesByRoomIdAsync(room.RoomId);
                var additionalAmenities = roomAmenities
                    .Where(ra => ra.Amenity.IsActive && ra.Amenity.AmenityType == nameof(AmenityType.Additional))
                    .Select(ra => new AmenityDto
                    {
                        AmenityId = ra.Amenity.AmenityId,
                        AmenityName = ra.Amenity.AmenityName,
                        Description = ra.Amenity.Description,
                        AmenityType = ra.Amenity.AmenityType,
                        IsActive = ra.Amenity.IsActive
                    }).ToList();

                // Concat Common + Additional amenities vào một list duy nhất
                var allAmenities = allCommonAmenities.Concat(additionalAmenities).ToList();

                var roomDto = new RoomWithImagesDto
                {
                    RoomId = room.RoomId,
                    RoomNumber = room.RoomNumber,
                    RoomTypeId = room.RoomTypeId,
                    RoomTypeName = roomType?.CodeValue,
                    BasePriceNight = room.BasePriceNight,
                    BasePriceHour = room.BasePriceHour,
                    StatusId = room.StatusId,
                    StatusName = status?.CodeValue,
                    Description = room.Description,
                    Images = _mapper.Map<List<MediumDto>>(images),
                    Amenities = allAmenities,
                    CreatedAt = room.CreatedAt,
                    UpdatedAt = room.UpdatedAt
                };

                roomDtos.Add(roomDto);
            }

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = new
                {
                    Items = roomDtos,
                    TotalRecords = totalRecords,
                    request.PageIndex,
                    request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize)
                },
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

            var roomType = await _unitOfWork.CommonCodes.GetByIdAsync(room.RoomTypeId);
            var status = await _unitOfWork.CommonCodes.GetByIdAsync(room.StatusId);
            
            var images = (await _unitOfWork.Mediums.FindAsync(m =>
                m.ReferenceTable == "Room" && m.ReferenceKey == roomId.ToString()))
                .OrderBy(m => m.DisplayOrder)
                .ToList();

            // Lấy tất cả Common amenities (tự động hiển thị)
            var commonAmenities = (await _unitOfWork.Amenities.GetByTypeAsync(nameof(AmenityType.Common), isActive: true))
                .Select(a => new AmenityDto
                {
                    AmenityId = a.AmenityId,
                    AmenityName = a.AmenityName,
                    Description = a.Description,
                    AmenityType = a.AmenityType,
                    IsActive = a.IsActive
                }).ToList();

            // Lấy Additional amenities đã được add vào room này
            var roomAmenities = await _unitOfWork.RoomAmenities.GetAmenitiesByRoomIdAsync(roomId);
            var additionalAmenities = roomAmenities
                .Where(ra => ra.Amenity.IsActive && ra.Amenity.AmenityType == nameof(AmenityType.Additional))
                .Select(ra => new AmenityDto
                {
                    AmenityId = ra.Amenity.AmenityId,
                    AmenityName = ra.Amenity.AmenityName,
                    Description = ra.Amenity.Description,
                    AmenityType = ra.Amenity.AmenityType,
                    IsActive = ra.Amenity.IsActive
                }).ToList();

            // Concat Common + Additional amenities vào một list duy nhất
            var allAmenities = commonAmenities.Concat(additionalAmenities).ToList();

            var roomDto = new RoomWithImagesDto
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomNumber,
                RoomTypeId = room.RoomTypeId,
                RoomTypeName = roomType?.CodeValue,
                BasePriceNight = room.BasePriceNight,
                BasePriceHour = room.BasePriceHour,
                StatusId = room.StatusId,
                StatusName = status?.CodeValue,
                Description = room.Description,
                Images = _mapper.Map<List<MediumDto>>(images),
                Amenities = allAmenities,
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
            // Kiểm tra số phòng đã tồn tại
            var existingRoom = (await _unitOfWork.Rooms.FindAsync(r => r.RoomNumber == request.RoomNumber)).FirstOrDefault();
            if (existingRoom != null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.EXISTED,
                    Message = string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Số phòng"),
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Kiểm tra RoomType tồn tại
            var roomType = await _unitOfWork.CommonCodes.GetByIdAsync(request.RoomTypeId);
            if (roomType == null || roomType.CodeType != "RoomType")
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
                RoomNumber = request.RoomNumber,
                RoomTypeId = request.RoomTypeId,
                BasePriceNight = request.BasePriceNight,
                BasePriceHour = request.BasePriceHour,
                StatusId = request.StatusId,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            await _unitOfWork.Rooms.AddAsync(room);
            await _unitOfWork.SaveChangesAsync();

            // Thêm images nếu có
            if (request.ImageUrls != null && request.ImageUrls.Any())
            {
                int order = 0;
                foreach (var imageUrl in request.ImageUrls)
                {
                    var medium = new Medium
                    {
                        ReferenceKey = room.RoomId.ToString(),
                        ReferenceTable = "Room",
                        FilePath = imageUrl,
                        Description = $"Room {room.RoomNumber} Image",
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
                Message = "Thêm phòng thành công",
                Data = new { room.RoomId, room.RoomNumber },
                StatusCode = StatusCodes.Status200OK
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

            // Kiểm tra số phòng mới có trùng không
            if (!string.IsNullOrWhiteSpace(request.RoomNumber) && request.RoomNumber != room.RoomNumber)
            {
                var existingRoom = (await _unitOfWork.Rooms.FindAsync(r => r.RoomNumber == request.RoomNumber)).FirstOrDefault();
                if (existingRoom != null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.EXISTED,
                        Message = string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Số phòng"),
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
                room.RoomNumber = request.RoomNumber;
            }

            // Cập nhật các trường
            if (request.RoomTypeId.HasValue) room.RoomTypeId = request.RoomTypeId.Value;
            if (request.BasePriceNight.HasValue) room.BasePriceNight = request.BasePriceNight.Value;
            if (request.BasePriceHour.HasValue) room.BasePriceHour = request.BasePriceHour.Value;
            if (request.StatusId.HasValue) room.StatusId = request.StatusId.Value;
            if (request.Description != null) room.Description = request.Description;

            room.UpdatedAt = DateTime.UtcNow;
            room.UpdatedBy = userId;

            await _unitOfWork.Rooms.UpdateAsync(room);
            await _unitOfWork.SaveChangesAsync();

            // Cập nhật images nếu có
            if (request.ImageUrls != null)
            {
                // Xóa images cũ
                var oldImages = await _unitOfWork.Mediums.FindAsync(m =>
                    m.ReferenceTable == "Room" && m.ReferenceKey == room.RoomId.ToString());
                foreach (var img in oldImages)
                {
                    await _unitOfWork.Mediums.DeleteAsync(img);
                }

                // Thêm images mới
                int order = 0;
                foreach (var imageUrl in request.ImageUrls)
                {
                    var medium = new Medium
                    {
                        ReferenceKey = room.RoomId.ToString(),
                        ReferenceTable = "Room",
                        FilePath = imageUrl,
                        Description = $"Room {room.RoomNumber} Image",
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
                Message = "Cập nhật phòng thành công",
                Data = new { room.RoomId, room.RoomNumber },
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

            // Xóa images liên quan
            var images = await _unitOfWork.Mediums.FindAsync(m =>
                m.ReferenceTable == "Room" && m.ReferenceKey == roomId.ToString());
            foreach (var img in images)
            {
                await _unitOfWork.Mediums.DeleteAsync(img);
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

        #region Room Type Operations

        public async Task<ResultModel> GetRoomTypeListAsync(GetRoomTypeListRequest request)
        {
            var query = _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "RoomType");
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
                    rt.CodeValue.Contains(request.Search) ||
                    rt.CodeName.Contains(request.Search) ||
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
                roomTypes = roomTypes.OrderBy(rt => rt.DisplayOrder);
            }

            var totalRecords = roomTypes.Count();

            var pagedRoomTypes = roomTypes
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Map với images và đếm số phòng
            var roomTypeDtos = new List<RoomTypeWithImagesDto>();
            foreach (var rt in pagedRoomTypes)
            {
                var images = (await _unitOfWork.Mediums.FindAsync(m =>
                    m.ReferenceTable == "RoomType" && m.ReferenceKey == rt.CodeId.ToString()))
                    .OrderBy(m => m.DisplayOrder)
                    .ToList();

                var totalRooms = (await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == rt.CodeId)).Count();

                var dto = new RoomTypeWithImagesDto
                {
                    CodeId = rt.CodeId,
                    RoomTypeName = rt.CodeValue,
                    Description = rt.Description,
                    IsActive = rt.IsActive,
                    DisplayOrder = rt.DisplayOrder,
                    Images = _mapper.Map<List<MediumDto>>(images),
                    TotalRooms = totalRooms,
                    CreatedAt = rt.CreatedAt,
                    UpdatedAt = rt.UpdatedAt
                };

                roomTypeDtos.Add(dto);
            }

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = new
                {
                    Items = roomTypeDtos,
                    TotalRecords = totalRecords,
                    request.PageIndex,
                    request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize)
                },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetRoomTypeDetailAsync(int roomTypeId)
        {
            var roomType = await _unitOfWork.CommonCodes.GetByIdAsync(roomTypeId);
            if (roomType == null || roomType.CodeType != "RoomType")
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
                CodeId = roomType.CodeId,
                RoomTypeName = roomType.CodeValue,
                Description = roomType.Description,
                IsActive = roomType.IsActive,
                DisplayOrder = roomType.DisplayOrder,
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
            // Kiểm tra tên loại phòng đã tồn tại
            var existing = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "RoomType" && c.CodeValue == request.RoomTypeName)).FirstOrDefault();

            if (existing != null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.EXISTED,
                    Message = string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Tên loại phòng"),
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Tạo RoomType mới
            var roomType = new CommonCode
            {
                CodeType = "RoomType",
                CodeValue = request.RoomTypeName,
                CodeName = request.RoomTypeName.Replace(" ", ""),
                Description = request.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            await _unitOfWork.CommonCodes.AddAsync(roomType);
            await _unitOfWork.SaveChangesAsync();

            // Thêm images nếu có
            if (request.ImageUrls != null && request.ImageUrls.Any())
            {
                int order = 0;
                foreach (var imageUrl in request.ImageUrls)
                {
                    var medium = new Medium
                    {
                        ReferenceKey = roomType.CodeId.ToString(),
                        ReferenceTable = "RoomType",
                        FilePath = imageUrl,
                        Description = $"RoomType {request.RoomTypeName} Image",
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
                Data = new { roomType.CodeId, RoomTypeName = roomType.CodeValue },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> UpdateRoomTypeAsync(UpdateRoomTypeRequest request, int userId)
        {
            var roomType = await _unitOfWork.CommonCodes.GetByIdAsync(request.RoomTypeId);
            if (roomType == null || roomType.CodeType != "RoomType")
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Loại phòng"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Cập nhật các trường
            if (!string.IsNullOrWhiteSpace(request.RoomTypeName))
            {
                roomType.CodeValue = request.RoomTypeName;
                roomType.CodeName = request.RoomTypeName.Replace(" ", "");
            }
            if (request.Description != null) roomType.Description = request.Description;
            if (request.IsActive.HasValue) roomType.IsActive = request.IsActive.Value;

            roomType.UpdatedAt = DateTime.UtcNow;
            roomType.UpdatedBy = userId;

            await _unitOfWork.CommonCodes.UpdateAsync(roomType);
            await _unitOfWork.SaveChangesAsync();

            // Cập nhật images nếu có
            if (request.ImageUrls != null)
            {
                // Xóa images cũ
                var oldImages = await _unitOfWork.Mediums.FindAsync(m =>
                    m.ReferenceTable == "RoomType" && m.ReferenceKey == roomType.CodeId.ToString());
                foreach (var img in oldImages)
                {
                    await _unitOfWork.Mediums.DeleteAsync(img);
                }

                // Thêm images mới
                int order = 0;
                foreach (var imageUrl in request.ImageUrls)
                {
                    var medium = new Medium
                    {
                        ReferenceKey = roomType.CodeId.ToString(),
                        ReferenceTable = "RoomType",
                        FilePath = imageUrl,
                        Description = $"RoomType {roomType.CodeValue} Image",
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
                Message = "Cập nhật loại phòng thành công",
                Data = new { roomType.CodeId, RoomTypeName = roomType.CodeValue },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> DeleteRoomTypeAsync(int roomTypeId, int userId)
        {
            var roomType = await _unitOfWork.CommonCodes.GetByIdAsync(roomTypeId);
            if (roomType == null || roomType.CodeType != "RoomType")
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Loại phòng"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Kiểm tra có phòng nào đang sử dụng loại phòng này không
            var roomsUsingType = await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == roomTypeId);
            if (roomsUsingType.Any())
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.INVALID,
                    Message = "Không thể xóa loại phòng này vì đang có phòng sử dụng",
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

            await _unitOfWork.CommonCodes.DeleteAsync(roomType);
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
    }
}

