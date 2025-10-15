using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.EmployeeModel;
using AppBackend.Services.Services.EmployeeServices;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing employees
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        /// <summary>
        /// Lấy chi tiết nhân viên
        /// </summary>
        /// <param name="employeeId">ID của nhân viên</param>
        /// <returns>Thông tin chi tiết nhân viên</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy nhân viên</response>
        [HttpGet("{employeeId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetEmployeeDetail(int employeeId)
        {
            var result = await _employeeService.GetEmployeeDetailAsync(employeeId);
            if (!result.IsSuccess)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách nhân viên với phân trang
        /// </summary>
        /// <param name="request">Thông tin phân trang và lọc</param>
        /// <returns>Danh sách nhân viên</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetEmployeeList([FromQuery] GetEmployeeListRequest request)
        {
            var result = await _employeeService.GetEmployeeListAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Thêm nhân viên mới
        /// </summary>
        /// <param name="request">Thông tin nhân viên mới</param>
        /// <returns>Thông tin nhân viên đã thêm</returns>
        /// <response code="200">Thêm nhân viên thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ResultModel 
                { 
                    IsSuccess = false, 
                    Message = "Dữ liệu không hợp lệ",
                    StatusCode = 400
                });

            var result = await _employeeService.AddEmployeeAsync(request);
            if (!result.IsSuccess)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        /// <param name="employeeId">ID của nhân viên</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Thông tin nhân viên đã cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="404">Không tìm thấy nhân viên</response>
        [HttpPut("{employeeId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateEmployee(int employeeId, [FromBody] UpdateEmployeeRequest request)
        {
            request.EmployeeId = employeeId;
            var result = await _employeeService.UpdateEmployeeAsync(request);
            if (!result.IsSuccess)
            {
                if (result.ResponseCode == "NOT_FOUND")
                    return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Khoá/Mở khoá tài khoản nhân viên
        /// </summary>
        /// <param name="employeeId">ID của nhân viên</param>
        /// <param name="request">Trạng thái khoá tài khoản</param>
        /// <returns>Kết quả thực hiện</returns>
        /// <response code="200">Thực hiện thành công</response>
        /// <response code="404">Không tìm thấy nhân viên</response>
        [HttpPatch("{employeeId}/ban")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BanEmployee(int employeeId, [FromBody] BanEmployeeRequest request)
        {
            request.EmployeeId = employeeId;
            var result = await _employeeService.BanEmployeeAsync(request);
            if (!result.IsSuccess)
                return NotFound(result);
            return Ok(result);
        }
    }
}

