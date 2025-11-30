using AppBackend.Services.ApiModels.RoomModel;
using AppBackend.Services.Services.RoomServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// API quản lý quan hệ giữa Room và Amenity - Chỉ CRUD đơn giản cho Admin
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Receptionist,Admin,Manager")]
    public class RoomAmenityController : BaseApiController
    {
        private readonly IRoomAmenityService _roomAmenityService;

        public RoomAmenityController(IRoomAmenityService roomAmenityService)
        {
            _roomAmenityService = roomAmenityService;
        }

        /// <summary>
        /// [ADMIN] Lấy danh sách tiện ích của một phòng cụ thể
        /// </summary>
        /// <param name="roomId">ID của phòng</param>
        /// <returns>Danh sách tiện ích</returns>
        [HttpGet("room/{roomId}")]
        public async Task<IActionResult> GetRoomAmenities(int roomId)
        {
            var result = await _roomAmenityService.GetRoomAmenitiesAsync(roomId);
            return HandleResult(result);
        }

        /// <summary>
        /// [ADMIN] Thêm một tiện ích vào phòng
        /// </summary>
        /// <param name="request">RoomId và AmenityId</param>
        /// <returns>Kết quả thực hiện</returns>
        [HttpPost]
        public async Task<IActionResult> AddRoomAmenity([FromBody] AddRoomAmenityRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _roomAmenityService.AddRoomAmenityAsync(request, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// [ADMIN] Xóa một tiện ích khỏi phòng
        /// </summary>
        /// <param name="roomId">ID của phòng</param>
        /// <param name="amenityId">ID của tiện ích</param>
        /// <returns>Kết quả thực hiện</returns>
        [HttpDelete("room/{roomId}/amenity/{amenityId}")]
        public async Task<IActionResult> DeleteRoomAmenity(int roomId, int amenityId)
        {
            var result = await _roomAmenityService.DeleteRoomAmenityAsync(roomId, amenityId);
            return HandleResult(result);
        }

        /// <summary>
        /// [ADMIN] Cập nhật toàn bộ danh sách tiện ích cho một phòng (batch update)
        /// </summary>
        /// <param name="roomId">ID của phòng</param>
        /// <param name="request">Danh sách AmenityIds mới (sẽ thay thế toàn bộ)</param>
        /// <returns>Kết quả thực hiện</returns>
        [HttpPut("room/{roomId}")]
        public async Task<IActionResult> UpdateRoomAmenities(int roomId, [FromBody] UpdateRoomAmenitiesRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            request.RoomId = roomId;
            var result = await _roomAmenityService.UpdateRoomAmenitiesAsync(request, CurrentUserId);
            return HandleResult(result);
        }
    }
}
