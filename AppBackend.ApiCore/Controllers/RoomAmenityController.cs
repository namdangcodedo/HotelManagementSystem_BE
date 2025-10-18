using AppBackend.Services.ApiModels.RoomModel;
using AppBackend.Services.Services.RoomServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomAmenityController : BaseApiController
    {
        private readonly IRoomAmenityService _roomAmenityService;

        public RoomAmenityController(IRoomAmenityService roomAmenityService)
        {
            _roomAmenityService = roomAmenityService;
        }

        #region GET - Public endpoints (No authentication required)

        /// <summary>
        /// Lấy danh sách tiện ích của một phòng
        /// </summary>
        /// <param name="roomId">ID của phòng</param>
        /// <param name="includeSelection">True: trả về tất cả amenities kèm trạng thái đã chọn/chưa chọn (dùng cho UI form)</param>
        [HttpGet("room/{roomId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoomAmenities(int roomId, [FromQuery] bool includeSelection = false)
        {
            var result = includeSelection
                ? await _roomAmenityService.GetRoomAmenitiesWithSelectionAsync(roomId)
                : await _roomAmenityService.GetRoomAmenitiesAsync(roomId);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách phòng có tiện ích cụ thể
        /// </summary>
        [HttpGet("amenity/{amenityId}/rooms")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoomsByAmenity(int amenityId)
        {
            var result = await _roomAmenityService.GetRoomsByAmenityAsync(amenityId);
            return StatusCode(result.StatusCode, result);
        }

        #endregion

        #region POST/PUT/DELETE - Require Admin or Manager role

        /// <summary>
        /// Cập nhật danh sách tiện ích cho phòng (sync/replace toàn bộ)
        /// </summary>
        /// <param name="roomId">ID của phòng</param>
        /// <param name="request">Danh sách AmenityIds</param>
        [HttpPut("room/{roomId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateRoomAmenities(int roomId, [FromBody] SyncRoomAmenitiesRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            request.RoomId = roomId;
            var result = await _roomAmenityService.SyncRoomAmenitiesAsync(request, userId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Thêm/xóa/toggle một tiện ích cho phòng
        /// </summary>
        /// <param name="request">RoomId và AmenityId</param>
        /// <param name="action">add: thêm, remove: xóa, toggle: tự động thêm/xóa (mặc định)</param>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ManageRoomAmenity(
            [FromBody] AddRoomAmenityRequest request, 
            [FromQuery] string action = "toggle")
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var result = action.ToLower() switch
            {
                "add" => await _roomAmenityService.AddRoomAmenityAsync(request, userId),
                "remove" => await _roomAmenityService.RemoveRoomAmenityAsync(
                    new RemoveRoomAmenityRequest { RoomId = request.RoomId, AmenityId = request.AmenityId }, userId),
                _ => await _roomAmenityService.ToggleRoomAmenityAsync(request, userId)
            };
            
            return StatusCode(result.StatusCode, result);
        }

        #endregion
    }
}
