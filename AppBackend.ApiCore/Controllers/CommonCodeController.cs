using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.CommonCodeModel;
using AppBackend.Services.Services.CommonCodeServices;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing common codes
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CommonCodeController : ControllerBase
    {
        private readonly ICommonCodeService _commonCodeService;

        public CommonCodeController(ICommonCodeService commonCodeService)
        {
            _commonCodeService = commonCodeService;
        }

        /// <summary>
        /// Lấy danh sách tất cả các loại mã (CodeType)
        /// </summary>
        /// <returns>Danh sách các CodeType và số lượng</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet("types")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCodeTypeList()
        {
            var result = await _commonCodeService.GetCodeTypeListAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách Common Code theo loại (CodeType)
        /// </summary>
        /// <param name="codeType">Loại mã (VD: RoomType, Status, EmployeeType...)</param>
        /// <returns>Danh sách Common Code theo loại</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet("by-type/{codeType}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCommonCodesByType(string codeType)
        {
            var result = await _commonCodeService.GetCommonCodesByTypeAsync(codeType);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách Common Code với phân trang và tìm kiếm
        /// </summary>
        /// <param name="request">Thông tin phân trang và lọc</param>
        /// <returns>Danh sách Common Code</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetCommonCodeList([FromQuery] GetCommonCodeListRequest request)
        {
            var result = await _commonCodeService.GetCommonCodeListAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết một Common Code
        /// </summary>
        /// <param name="codeId">ID của Common Code</param>
        /// <returns>Thông tin chi tiết Common Code</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy Common Code</response>
        [HttpGet("{codeId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetCommonCodeById(int codeId)
        {
            var result = await _commonCodeService.GetCommonCodeByIdAsync(codeId);
            if (!result.IsSuccess)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Thêm Common Code mới
        /// </summary>
        /// <param name="request">Thông tin Common Code mới</param>
        /// <returns>Thông tin Common Code đã thêm</returns>
        /// <response code="200">Thêm Common Code thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddCommonCode([FromBody] AddCommonCodeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ResultModel
                {
                    IsSuccess = false,
                    Message = "Dữ liệu không hợp lệ",
                    StatusCode = 400
                });

            var result = await _commonCodeService.AddCommonCodeAsync(request);
            if (!result.IsSuccess)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật thông tin Common Code
        /// </summary>
        /// <param name="codeId">ID của Common Code</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Thông tin Common Code đã cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="404">Không tìm thấy Common Code</response>
        [HttpPut("{codeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCommonCode(int codeId, [FromBody] UpdateCommonCodeRequest request)
        {
            request.CodeId = codeId;
            var result = await _commonCodeService.UpdateCommonCodeAsync(request);
            if (!result.IsSuccess)
            {
                if (result.ResponseCode == "NOT_FOUND")
                    return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Xóa Common Code
        /// </summary>
        /// <param name="codeId">ID của Common Code</param>
        /// <returns>Kết quả thực hiện</returns>
        /// <response code="200">Xóa thành công</response>
        /// <response code="404">Không tìm thấy Common Code</response>
        [HttpDelete("{codeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCommonCode(int codeId)
        {
            var result = await _commonCodeService.DeleteCommonCodeAsync(codeId);
            if (!result.IsSuccess)
                return NotFound(result);
            return Ok(result);
        }
    }
}

