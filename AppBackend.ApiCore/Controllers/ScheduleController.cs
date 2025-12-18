using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.ApiModels.ScheduleModel;
using AppBackend.Services.Services.ScheduleServices;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing employee schedules
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ScheduleController : BaseApiController
    {
        private readonly IScheduleService _scheduleService;

        public ScheduleController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        /// <summary>
        /// Lấy lịch làm việc theo khoảng thời gian
        /// </summary>
        /// <param name="request">FromDate và ToDate (format yyyyMMdd)</param>
        /// <returns>Lịch làm việc theo khoảng thời gian với các ca làm việc</returns>
        /// <response code="200">Lấy lịch làm việc thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        [HttpPost("schedules")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetSchedules([FromForm] GetWeeklyScheduleRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _scheduleService.GetWeeklyScheduleAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Thêm lịch làm việc mới cho nhân viên
        /// </summary>
        /// <param name="request">Thông tin lịch làm việc mới</param>
        /// <returns>Kết quả thêm lịch làm việc</returns>
        /// <response code="201">Thêm lịch làm việc thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="404">Không tìm thấy nhân viên</response>
        /// <response code="409">Nhân viên đã có lịch làm việc trùng thời gian</response>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddSchedule([FromForm] AddScheduleRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _scheduleService.AddScheduleAsync(request, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật lịch làm việc
        /// </summary>
        /// <param name="scheduleId">ID của lịch làm việc</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Kết quả cập nhật lịch làm việc</returns>
        /// <response code="200">Cập nhật lịch làm việc thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="404">Không tìm thấy lịch làm việc</response>
        /// <response code="409">Nhân viên đã có lịch làm việc trùng thời gian</response>
        [HttpPut("{scheduleId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateSchedule(int scheduleId, [FromForm] UpdateScheduleRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            request.ScheduleId = scheduleId;
            var result = await _scheduleService.UpdateScheduleAsync(request, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// Xoá lịch làm việc
        /// </summary>
        /// <param name="scheduleId">ID của lịch làm việc</param>
        /// <returns>Kết quả xoá lịch làm việc</returns>
        /// <response code="200">Xoá lịch làm việc thành công</response>
        /// <response code="404">Không tìm thấy lịch làm việc</response>
        [HttpDelete("{scheduleId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteSchedule(int scheduleId)
        {
            var result = await _scheduleService.DeleteScheduleAsync(scheduleId);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy danh sách nhân viên có thể thêm vào ca làm việc (không bị trùng lịch)
        /// </summary>
        /// <param name="request">Thông tin ca làm việc (ShiftDate, StartTime, EndTime, EmployeeTypeId)</param>
        /// <returns>Danh sách nhân viên có thể thêm vào ca</returns>
        /// <response code="200">Lấy danh sách nhân viên thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        [HttpGet("available-employees")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAvailableEmployees([FromQuery] CheckAvailableEmployeesRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _scheduleService.GetAvailableEmployeesAsync(request);
            return HandleResult(result);
        }
    }
}
