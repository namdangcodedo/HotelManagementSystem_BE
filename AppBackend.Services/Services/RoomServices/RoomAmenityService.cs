using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.RoomModel;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.RoomServices
{
    /// <summary>
    /// Service quản lý quan hệ giữa Room và Amenity - Chỉ CRUD đơn giản
    /// </summary>
    public class RoomAmenityService : IRoomAmenityService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoomAmenityService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Lấy danh sách amenities của một phòng cụ thể
        /// </summary>
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
                    RoomName = room.RoomName,
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

        /// <summary>
        /// Thêm amenity vào phòng
        /// </summary>
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

            // Kiểm tra đã tồn tại chưa
            var existing = (await _unitOfWork.RoomAmenities.FindAsync(ra =>
                ra.RoomId == request.RoomId && ra.AmenityId == request.AmenityId)).FirstOrDefault();
            
            if (existing != null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.EXISTED,
                    Message = "Tiện ích này đã được thêm vào phòng",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Thêm mới
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

        /// <summary>
        /// Xóa amenity khỏi phòng
        /// </summary>
        public async Task<ResultModel> DeleteRoomAmenityAsync(int roomId, int amenityId)
        {
            var roomAmenity = (await _unitOfWork.RoomAmenities.FindAsync(ra =>
                ra.RoomId == roomId && ra.AmenityId == amenityId)).FirstOrDefault();

            if (roomAmenity == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Không tìm thấy quan hệ phòng-tiện ích này",
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

        /// <summary>
        /// Cập nhật toàn bộ amenities cho một phòng (batch update)
        /// </summary>
        public async Task<ResultModel> UpdateRoomAmenitiesAsync(UpdateRoomAmenitiesRequest request, int userId)
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

            // Xóa tất cả amenities cũ
            var oldAmenities = await _unitOfWork.RoomAmenities.GetAmenitiesByRoomIdAsync(request.RoomId);
            foreach (var ra in oldAmenities)
            {
                await _unitOfWork.RoomAmenities.DeleteAsync(ra);
            }

            // Thêm amenities mới
            if (request.AmenityIds != null && request.AmenityIds.Any())
            {
                foreach (var amenityId in request.AmenityIds)
                {
                    // Kiểm tra amenity có tồn tại không
                    var amenity = await _unitOfWork.Amenities.GetByIdAsync(amenityId);
                    if (amenity == null)
                    {
                        continue; // Bỏ qua amenity không tồn tại
                    }

                    var roomAmenity = new RoomAmenity
                    {
                        RoomId = request.RoomId,
                        AmenityId = amenityId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    };
                    await _unitOfWork.RoomAmenities.AddAsync(roomAmenity);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Cập nhật danh sách tiện ích cho phòng thành công",
                StatusCode = StatusCodes.Status200OK
            };
        }
    }
}
