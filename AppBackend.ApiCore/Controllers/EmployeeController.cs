using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public class EmployeeController : BaseApiController
    {
        private readonly EmployeeService _employeeService;

        public EmployeeController(EmployeeService employeeService)
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
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy danh sách nhân viên với phân trang
        /// </summary>
        /// <param name="request">Thông tin phân trang và lọc</param>
        /// <returns>Danh sách nhân viên</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetEmployeeList([FromQuery] GetEmployeeRequest request)
        {
            var result = await _employeeService.GetEmployeeListAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Thêm nhân viên mới
        /// </summary>
        /// <param name="request">Thông tin nhân viên mới</param>
        /// <returns>Thông tin nhân viên đã thêm</returns>
        /// <response code="201">Thêm nhân viên thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _employeeService.AddEmployeeAsync(request);
            return HandleResult(result);
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
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            request.EmployeeId = employeeId;
            var result = await _employeeService.UpdateEmployeeAsync(request);
            return HandleResult(result);
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
            return HandleResult(result);
        }
    }
}
