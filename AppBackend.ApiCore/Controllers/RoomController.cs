using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.ApiModels.RoomModel;
using AppBackend.Services.Services.RoomServices;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing rooms and room types
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RoomController : BaseApiController
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        #region ROOM TYPE SEARCH - FOR CUSTOMER

        /// <summary>
        /// [PUBLIC] Tìm kiếm loại phòng cho customer với các filter (giá, số người, loại giường...)
        /// </summary>
        /// <param name="request">Thông tin tìm kiếm và filter</param>
        /// <returns>Danh sách loại phòng phù hợp với availability</returns>
        /// <response code="200">Tìm kiếm thành công</response>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchRoomTypes([FromQuery] SearchRoomTypeRequest request)
        {
            var result = await _roomService.SearchRoomTypesAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// [PUBLIC] Lấy chi tiết loại phòng cho customer (có thể kiểm tra availability)
        /// </summary>
        /// <param name="id">ID của loại phòng</param>
        /// <param name="checkInDate">Ngày check-in (optional)</param>
        /// <param name="checkOutDate">Ngày check-out (optional)</param>
        /// <returns>Thông tin chi tiết loại phòng kèm availability</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy loại phòng</response>
        [HttpGet("search/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoomTypeDetailForCustomer(int id, [FromQuery] DateTime? checkInDate = null, [FromQuery] DateTime? checkOutDate = null)
        {
            var result = await _roomService.GetRoomTypeDetailForCustomerAsync(id, checkInDate, checkOutDate);
            return HandleResult(result);
        }

        #endregion

        #region ROOM TYPE CRUD - FOR ADMIN

        /// <summary>
        /// [ADMIN] Lấy danh sách loại phòng với phân trang
        /// </summary>
        /// <param name="request">Thông tin phân trang và lọc</param>
        /// <returns>Danh sách loại phòng với hình ảnh và số lượng phòng</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet("types")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetRoomTypeList([FromQuery] GetRoomTypeListRequest request)
        {
            var result = await _roomService.GetRoomTypeListAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// [ADMIN] Lấy chi tiết một loại phòng
        /// </summary>
        /// <param name="id">ID của loại phòng</param>
        /// <returns>Thông tin chi tiết loại phòng kèm hình ảnh</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy loại phòng</response>
        [HttpGet("types/{id}")]
        public async Task<IActionResult> GetRoomTypeDetail(int id)
        {
            var result = await _roomService.GetRoomTypeDetailAsync(id);
            return HandleResult(result);
        }

        /// <summary>
        /// [ADMIN] Thêm loại phòng mới
        /// </summary>
        /// <param name="request">Thông tin loại phòng mới (bao gồm danh sách URL hình ảnh)</param>
        /// <returns>Thông tin loại phòng đã thêm</returns>
        /// <response code="201">Thêm loại phòng thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        [HttpPost("types")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddRoomType([FromBody] AddRoomTypeRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _roomService.AddRoomTypeAsync(request, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// [ADMIN] Cập nhật thông tin loại phòng
        /// </summary>
        /// <param name="id">ID của loại phòng</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Thông tin loại phòng đã cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="404">Không tìm thấy loại phòng</response>
        [HttpPut("types/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateRoomType(int id, [FromBody] UpdateRoomTypeRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            request.RoomTypeId = id;
            var result = await _roomService.UpdateRoomTypeAsync(request, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// [ADMIN] Xóa loại phòng
        /// </summary>
        /// <param name="id">ID của loại phòng</param>
        /// <returns>Kết quả thực hiện</returns>
        /// <response code="200">Xóa thành công</response>
        /// <response code="404">Không tìm thấy loại phòng</response>
        /// <response code="400">Không thể xóa vì còn phòng đang sử dụng loại này</response>
        [HttpDelete("types/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRoomType(int id)
        {
            var result = await _roomService.DeleteRoomTypeAsync(id, CurrentUserId);
            return HandleResult(result);
        }

        #endregion

        #region ROOM CRUD - FOR ADMIN ONLY

        /// <summary>
        /// [ADMIN] Lấy danh sách phòng cụ thể với phân trang và lọc
        /// </summary>
        /// <param name="request">Thông tin phân trang và lọc</param>
        /// <returns>Danh sách phòng với hình ảnh</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet("rooms")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetRoomList([FromQuery] GetRoomListRequest request)
        {
            var result = await _roomService.GetRoomListAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// [ADMIN] Lấy chi tiết một phòng cụ thể
        /// </summary>
        /// <param name="id">ID của phòng</param>
        /// <returns>Thông tin chi tiết phòng kèm hình ảnh</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy phòng</response>
        [HttpGet("rooms/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetRoomDetail(int id)
        {
            var result = await _roomService.GetRoomDetailAsync(id);
            return HandleResult(result);
        }

        /// <summary>
        /// [ADMIN] Thêm phòng mới
        /// </summary>
        /// <param name="request">Thông tin phòng mới (bao gồm danh sách URL hình ảnh)</param>
        /// <returns>Thông tin phòng đã thêm</returns>
        /// <response code="201">Thêm phòng thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        [HttpPost("rooms")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddRoom([FromBody] AddRoomRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _roomService.AddRoomAsync(request, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// [ADMIN] Cập nhật thông tin phòng
        /// </summary>
        /// <param name="id">ID của phòng</param>
        /// <param name="request">Thông tin cập nhật (bao gồm danh sách URL hình ảnh mới nếu có)</param>
        /// <returns>Thông tin phòng đã cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="404">Không tìm thấy phòng</response>
        [HttpPut("rooms/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            request.RoomId = id;
            var result = await _roomService.UpdateRoomAsync(request, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// [ADMIN] Xóa phòng
        /// </summary>
        /// <param name="id">ID của phòng</param>
        /// <returns>Kết quả thực hiện</returns>
        /// <response code="200">Xóa thành công</response>
        /// <response code="404">Không tìm thấy phòng</response>
        [HttpDelete("rooms/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var result = await _roomService.DeleteRoomAsync(id, CurrentUserId);
            return HandleResult(result);
        }

        #endregion
    }
}
