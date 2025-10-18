using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.RoomModel;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBackend.Services.Services.RoomServices
{
    public class RoomAmenityService : IRoomAmenityService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoomAmenityService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResultModel> GetRoomAmenitiesAsync(int roomId)
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

            var roomAmenities = await _unitOfWork.RoomAmenities.GetAmenitiesByRoomIdAsync(roomId);
            
            var amenityDtos = new List<RoomAmenityDto>();
            foreach (var ra in roomAmenities)
            {
                var dto = new RoomAmenityDto
                {
                    RoomId = ra.RoomId,
                    RoomNumber = room.RoomNumber,
                    AmenityId = ra.AmenityId,
                    AmenityName = ra.Amenity.AmenityName,
                    Description = ra.Amenity.Description,
                    AmenityType = ra.Amenity.AmenityType,
                    IsActive = ra.Amenity.IsActive,
                    CreatedAt = ra.CreatedAt
                };
                amenityDtos.Add(dto);
            }

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = amenityDtos,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetRoomsByAmenityAsync(int amenityId)
        {
            var amenity = await _unitOfWork.Amenities.GetByIdAsync(amenityId);
            if (amenity == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Tiện ích"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            var roomAmenities = await _unitOfWork.RoomAmenities.GetRoomsByAmenityIdAsync(amenityId);
            
            var roomDtos = new List<RoomAmenityDto>();
            foreach (var ra in roomAmenities)
            {
                var dto = new RoomAmenityDto
                {
                    RoomId = ra.RoomId,
                    RoomNumber = ra.Room.RoomNumber,
                    AmenityId = ra.AmenityId,
                    AmenityName = amenity.AmenityName,
                    Description = amenity.Description,
                    AmenityType = amenity.AmenityType,
                    IsActive = amenity.IsActive,
                    CreatedAt = ra.CreatedAt
                };
                roomDtos.Add(dto);
            }

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = roomDtos,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> AddRoomAmenityAsync(AddRoomAmenityRequest request, int userId)
        {
            // Kiểm tra Room tồn tại
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

            // Kiểm tra Amenity tồn tại
            var amenity = await _unitOfWork.Amenities.GetByIdAsync(request.AmenityId);
            if (amenity == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Tiện ích"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Kiểm tra quan hệ đã tồn tại
            var exists = await _unitOfWork.RoomAmenities.ExistsAsync(request.RoomId, request.AmenityId);
            if (exists)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.EXISTED,
                    Message = "Tiện ích này đã được thêm vào phòng",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Tạo RoomAmenity mới
            var roomAmenity = new RoomAmenity
            {
                RoomId = request.RoomId,
                AmenityId = request.AmenityId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            await _unitOfWork.RoomAmenities.AddAsync(roomAmenity);
            await _unitOfWork.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Thêm tiện ích vào phòng thành công",
                Data = new { request.RoomId, request.AmenityId },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> AddMultipleRoomAmenitiesAsync(AddMultipleRoomAmenitiesRequest request, int userId)
        {
            // Kiểm tra Room tồn tại
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

            var addedCount = 0;
            var skippedCount = 0;
            var notFoundAmenities = new List<int>();

            foreach (var amenityId in request.AmenityIds)
            {
                // Kiểm tra Amenity tồn tại
                var amenity = await _unitOfWork.Amenities.GetByIdAsync(amenityId);
                if (amenity == null)
                {
                    notFoundAmenities.Add(amenityId);
                    continue;
                }

                // Kiểm tra quan hệ đã tồn tại
                var exists = await _unitOfWork.RoomAmenities.ExistsAsync(request.RoomId, amenityId);
                if (exists)
                {
                    skippedCount++;
                    continue;
                }

                // Tạo RoomAmenity mới
                var roomAmenity = new RoomAmenity
                {
                    RoomId = request.RoomId,
                    AmenityId = amenityId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                await _unitOfWork.RoomAmenities.AddAsync(roomAmenity);
                addedCount++;
            }

            await _unitOfWork.SaveChangesAsync();

            var message = $"Đã thêm {addedCount} tiện ích vào phòng";
            if (skippedCount > 0)
            {
                message += $", bỏ qua {skippedCount} tiện ích đã tồn tại";
            }
            if (notFoundAmenities.Any())
            {
                message += $", không tìm thấy {notFoundAmenities.Count} tiện ích";
            }

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = message,
                Data = new 
                { 
                    AddedCount = addedCount,
                    SkippedCount = skippedCount,
                    NotFoundAmenities = notFoundAmenities
                },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> RemoveRoomAmenityAsync(RemoveRoomAmenityRequest request, int userId)
        {
            var roomAmenity = await _unitOfWork.RoomAmenities.GetByRoomAndAmenityAsync(request.RoomId, request.AmenityId);
            if (roomAmenity == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Không tìm thấy tiện ích trong phòng này",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            await _unitOfWork.RoomAmenities.DeleteAsync(roomAmenity);
            await _unitOfWork.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Xóa tiện ích khỏi phòng thành công",
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> RemoveAllRoomAmenitiesAsync(int roomId, int userId)
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

            var roomAmenities = (await _unitOfWork.RoomAmenities.GetAmenitiesByRoomIdAsync(roomId)).ToList();
            foreach (var ra in roomAmenities)
            {
                await _unitOfWork.RoomAmenities.DeleteAsync(ra);
            }

            await _unitOfWork.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Đã xóa {roomAmenities.Count} tiện ích khỏi phòng",
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetRoomAmenitiesWithSelectionAsync(int roomId)
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

            // Lấy tất cả amenities hiện có
            var allAmenities = (await _unitOfWork.Amenities.GetAllAsync())
                .Where(a => a.IsActive)
                .ToList();

            // Lấy amenities đã được chọn cho phòng này
            var roomAmenities = await _unitOfWork.RoomAmenities.GetAmenitiesByRoomIdAsync(roomId);
            var selectedAmenityIds = roomAmenities.Select(ra => ra.AmenityId).ToHashSet();

            // Map sang DTO với trạng thái IsSelected
            var amenityDtos = allAmenities.Select(a => new AmenityWithSelectionDto
            {
                AmenityId = a.AmenityId,
                AmenityName = a.AmenityName,
                Description = a.Description,
                AmenityType = a.AmenityType,
                IsActive = a.IsActive,
                IsSelected = selectedAmenityIds.Contains(a.AmenityId)
            }).ToList();

            var result = new RoomAmenitiesWithSelectionDto
            {
                RoomId = roomId,
                RoomNumber = room.RoomNumber,
                Amenities = amenityDtos
            };

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> SyncRoomAmenitiesAsync(SyncRoomAmenitiesRequest request, int userId)
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

            // Xóa tất cả tiện ích hiện tại
            var existingAmenities = (await _unitOfWork.RoomAmenities.GetAmenitiesByRoomIdAsync(request.RoomId)).ToList();
            foreach (var ra in existingAmenities)
            {
                await _unitOfWork.RoomAmenities.DeleteAsync(ra);
            }

            // Thêm các tiện ích mới
            var addedCount = 0;
            var notFoundAmenities = new List<int>();

            foreach (var amenityId in request.AmenityIds)
            {
                var amenity = await _unitOfWork.Amenities.GetByIdAsync(amenityId);
                if (amenity == null)
                {
                    notFoundAmenities.Add(amenityId);
                    continue;
                }

                var roomAmenity = new RoomAmenity
                {
                    RoomId = request.RoomId,
                    AmenityId = amenityId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                await _unitOfWork.RoomAmenities.AddAsync(roomAmenity);
                addedCount++;
            }

            await _unitOfWork.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Đồng bộ thành công: {addedCount} tiện ích được thêm vào phòng",
                Data = new
                {
                    RoomId = request.RoomId,
                    AddedCount = addedCount,
                    RemovedCount = existingAmenities.Count,
                    NotFoundAmenities = notFoundAmenities
                },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> ToggleRoomAmenityAsync(AddRoomAmenityRequest request, int userId)
        {
            // Kiểm tra Room tồn tại
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

            // Kiểm tra Amenity tồn tại
            var amenity = await _unitOfWork.Amenities.GetByIdAsync(request.AmenityId);
            if (amenity == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Tiện ích"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Kiểm tra xem đã tồn tại chưa
            var exists = await _unitOfWork.RoomAmenities.GetByRoomAndAmenityAsync(request.RoomId, request.AmenityId);

            if (exists != null)
            {
                // Đã có -> Xóa
                await _unitOfWork.RoomAmenities.DeleteAsync(exists);
                await _unitOfWork.SaveChangesAsync();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = $"Đã bỏ chọn tiện ích '{amenity.AmenityName}' khỏi phòng {room.RoomNumber}",
                    Data = new { Action = "removed", request.RoomId, request.AmenityId },
                    StatusCode = StatusCodes.Status200OK
                };
            }
            else
            {
                // Chưa có -> Thêm
                var roomAmenity = new RoomAmenity
                {
                    RoomId = request.RoomId,
                    AmenityId = request.AmenityId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                await _unitOfWork.RoomAmenities.AddAsync(roomAmenity);
                await _unitOfWork.SaveChangesAsync();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = $"Đã chọn tiện ích '{amenity.AmenityName}' cho phòng {room.RoomNumber}",
                    Data = new { Action = "added", request.RoomId, request.AmenityId },
                    StatusCode = StatusCodes.Status200OK
                };
            }
        }
    }
}
