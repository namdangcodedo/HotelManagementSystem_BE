using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.Services.Services.AmenityServices;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing hotel amenities
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AmenityController : BaseApiController
    {
        private readonly IAmenityService _amenityService;
        
        public AmenityController(IAmenityService amenityService)
        {
            _amenityService = amenityService;
        }

        /// <summary>
        /// Lấy danh sách tiện ích với phân trang
        /// </summary>
        /// <param name="request">Thông tin phân trang và lọc</param>
        /// <returns>Danh sách tiện ích</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAmenityList([FromQuery] PagedAmenityRequestDto request)
        {
            var result = await _amenityService.GetAmenityPagedAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Lấy tất cả tiện ích (không phân trang)
        /// </summary>
        /// <param name="isActive">Lọc theo trạng thái hoạt động</param>
        /// <returns>Danh sách tất cả tiện ích</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllAmenities([FromQuery] bool? isActive)
        {
            var result = await _amenityService.GetAmenityListAsync(isActive);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết một tiện ích
        /// </summary>
        /// <param name="id">ID của tiện ích</param>
        /// <returns>Thông tin chi tiết tiện ích</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy tiện ích</response>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAmenityDetail(int id)
        {
            var result = await _amenityService.GetAmenityDetailAsync(id);
            return HandleResult(result);
        }

        /// <summary>
        /// Thêm tiện ích mới
        /// </summary>
        /// <param name="dto">Thông tin tiện ích mới</param>
        /// <returns>Thông tin tiện ích đã thêm</returns>
        /// <response code="201">Thêm tiện ích thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddAmenity([FromBody] AmenityDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");
            
            var result = await _amenityService.AddAmenityAsync(dto, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật thông tin tiện ích
        /// </summary>
        /// <param name="id">ID của tiện ích</param>
        /// <param name="dto">Thông tin cập nhật</param>
        /// <returns>Thông tin tiện ích đã cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="404">Không tìm thấy tiện ích</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateAmenity(int id, [FromBody] AmenityDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");
            
            dto.AmenityId = id;
            var result = await _amenityService.UpdateAmenityAsync(dto, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// Xóa tiện ích
        /// </summary>
        /// <param name="id">ID của tiện ích</param>
        /// <returns>Kết quả thực hiện</returns>
        /// <response code="200">Xóa thành công</response>
        /// <response code="404">Không tìm thấy tiện ích</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteAmenity(int id)
        {
            var result = await _amenityService.DeleteAmenityAsync(id, CurrentUserId);
            return HandleResult(result);
        }
    }
}
