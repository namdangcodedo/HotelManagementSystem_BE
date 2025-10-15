using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.RoomModel;
using AppBackend.Services.Services.RoomServices;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing rooms and room types
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        #region Room APIs

        /// <summary>
        /// Lấy danh sách phòng với phân trang và lọc
        /// </summary>
        /// <param name="request">Thông tin phân trang và lọc</param>
        /// <returns>Danh sách phòng với hình ảnh</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoomList([FromQuery] GetRoomListRequest request)
        {
            var result = await _roomService.GetRoomListAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết một phòng
        /// </summary>
        /// <param name="id">ID của phòng</param>
        /// <returns>Thông tin chi tiết phòng kèm hình ảnh</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy phòng</response>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoomDetail(int id)
        {
            var result = await _roomService.GetRoomDetailAsync(id);
            if (!result.IsSuccess)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Thêm phòng mới
        /// </summary>
        /// <param name="request">Thông tin phòng mới (bao gồm danh sách URL hình ảnh)</param>
        /// <returns>Thông tin phòng đã thêm</returns>
        /// <response code="200">Thêm phòng thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddRoom([FromBody] AddRoomRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _roomService.AddRoomAsync(request, userId);
            if (!result.IsSuccess)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật thông tin phòng
        /// </summary>
        /// <param name="id">ID của phòng</param>
        /// <param name="request">Thông tin cập nhật (bao gồm danh sách URL hình ảnh mới nếu có)</param>
        /// <returns>Thông tin phòng đã cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="404">Không tìm thấy phòng</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            request.RoomId = id;
            var result = await _roomService.UpdateRoomAsync(request, userId);
            if (!result.IsSuccess)
            {
                if (result.ResponseCode == "NOT_FOUND")
                    return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Xóa phòng
        /// </summary>
        /// <param name="id">ID của phòng</param>
        /// <returns>Kết quả thực hiện</returns>
        /// <response code="200">Xóa thành công</response>
        /// <response code="404">Không tìm thấy phòng</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _roomService.DeleteRoomAsync(id, userId);
            if (!result.IsSuccess)
                return NotFound(result);
            return Ok(result);
        }

        #endregion

        #region Room Type APIs

        /// <summary>
        /// Lấy danh sách loại phòng với phân trang
        /// </summary>
        /// <param name="request">Thông tin phân trang và lọc</param>
        /// <returns>Danh sách loại phòng với hình ảnh và số lượng phòng</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet("types")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoomTypeList([FromQuery] GetRoomTypeListRequest request)
        {
            var result = await _roomService.GetRoomTypeListAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết một loại phòng
        /// </summary>
        /// <param name="id">ID của loại phòng</param>
        /// <returns>Thông tin chi tiết loại phòng kèm hình ảnh</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy loại phòng</response>
        [HttpGet("types/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoomTypeDetail(int id)
        {
            var result = await _roomService.GetRoomTypeDetailAsync(id);
            if (!result.IsSuccess)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Thêm loại phòng mới
        /// </summary>
        /// <param name="request">Thông tin loại phòng mới (bao gồm danh sách URL hình ảnh)</param>
        /// <returns>Thông tin loại phòng đã thêm</returns>
        /// <response code="200">Thêm loại phòng thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        [HttpPost("types")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddRoomType([FromBody] AddRoomTypeRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _roomService.AddRoomTypeAsync(request, userId);
            if (!result.IsSuccess)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật thông tin loại phòng
        /// </summary>
        /// <param name="id">ID của loại phòng</param>
        /// <param name="request">Thông tin cập nhật (bao gồm danh sách URL hình ảnh mới nếu có)</param>
        /// <returns>Thông tin loại phòng đã cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="404">Không tìm thấy loại phòng</response>
        [HttpPut("types/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateRoomType(int id, [FromBody] UpdateRoomTypeRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            request.RoomTypeId = id;
            var result = await _roomService.UpdateRoomTypeAsync(request, userId);
            if (!result.IsSuccess)
            {
                if (result.ResponseCode == "NOT_FOUND")
                    return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Xóa loại phòng
        /// </summary>
        /// <param name="id">ID của loại phòng</param>
        /// <returns>Kết quả thực hiện</returns>
        /// <response code="200">Xóa thành công</response>
        /// <response code="400">Không thể xóa vì đang có phòng sử dụng</response>
        /// <response code="404">Không tìm thấy loại phòng</response>
        [HttpDelete("types/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRoomType(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _roomService.DeleteRoomTypeAsync(id, userId);
            if (!result.IsSuccess)
            {
                if (result.ResponseCode == "NOT_FOUND")
                    return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        #endregion
    }
}

