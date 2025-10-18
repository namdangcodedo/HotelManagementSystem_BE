using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.Services.Services.AmenityServices;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs quản lý tiện nghi khách sạn (Amenities)
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

        #region GET - Public endpoints (No authentication required)

        /// <summary>
        /// Lấy danh sách tiện nghi với phân trang và lọc
        /// </summary>
        /// <param name="request">Thông tin phân trang, tìm kiếm, lọc theo type (Common/Additional) và trạng thái</param>
        /// <returns>Danh sách tiện nghi có phân trang</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAmenityList([FromQuery] PagedAmenityRequestDto request)
        {
            var result = await _amenityService.GetAmenityPagedAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Lấy tất cả tiện nghi (không phân trang) - dùng cho dropdown/select
        /// </summary>
        /// <param name="isActive">Lọc theo trạng thái hoạt động (null = tất cả)</param>
        /// <param name="amenityType">Lọc theo loại tiện nghi: Common (cơ bản), Additional (bổ sung)</param>
        /// <returns>Danh sách tất cả tiện nghi</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllAmenities([FromQuery] bool? isActive, [FromQuery] string? amenityType)
        {
            var result = await _amenityService.GetAmenityListAsync(isActive, amenityType);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết một tiện nghi
        /// </summary>
        /// <param name="id">ID của tiện nghi</param>
        /// <returns>Thông tin chi tiết tiện nghi</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy tiện nghi</response>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAmenityDetail(int id)
        {
            var result = await _amenityService.GetAmenityDetailAsync(id);
            return HandleResult(result);
        }

        #endregion

        #region POST/PUT/DELETE - Require Admin or Manager role

        /// <summary>
        /// Thêm tiện nghi mới
        /// </summary>
        /// <param name="dto">Thông tin tiện nghi mới (AmenityName, Description, AmenityType: Common/Additional)</param>
        /// <returns>Thông tin tiện nghi đã thêm</returns>
        /// <response code="201">Thêm tiện nghi thành công</response>
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
        /// Cập nhật thông tin tiện nghi
        /// </summary>
        /// <param name="id">ID của tiện nghi</param>
        /// <param name="dto">Thông tin cập nhật (AmenityName, Description, AmenityType: Common/Additional, IsActive)</param>
        /// <returns>Thông tin tiện nghi đã cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="404">Không tìm thấy tiện nghi</response>
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
        /// Xóa tiện nghi (soft delete)
        /// </summary>
        /// <param name="id">ID của tiện nghi</param>
        /// <returns>Kết quả thực hiện</returns>
        /// <response code="200">Xóa thành công</response>
        /// <response code="404">Không tìm thấy tiện nghi</response>
        /// <response code="400">Không thể xóa tiện nghi đang được sử dụng</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteAmenity(int id)
        {
            var result = await _amenityService.DeleteAmenityAsync(id, CurrentUserId);
            return HandleResult(result);
        }

        #endregion
    }
}
