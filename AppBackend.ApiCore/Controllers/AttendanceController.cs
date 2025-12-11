using AppBackend.Services.ApiModels.AttendanceModel;
using AppBackend.Services.ApiModels.EmployeeModel;
using AppBackend.Services.Services.AttendanceServices;
using AppBackend.Services.Services.EmployeeServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing employees
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class AttendanceController : BaseApiController
    {
        private readonly IEmployeeService _employeeService;
        private readonly IAttendaceService _attendanceService;

        public AttendanceController(IEmployeeService employeeService, IAttendaceService attendaceService)
        {
            _employeeService = employeeService;
            _attendanceService = attendaceService;
        }

       
        [HttpPost("AttendInfo")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetEmployeeAttendInfo(GetAttendanceRequest request)
        {
            var result = await _attendanceService.GetEmployeeAttendInfo(request);
            return HandleResult(result);
        }

        [HttpPost("GetAttendance")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetEmployeeAttendance(GetAttendanceRequest request)
        {
            var result = await _attendanceService.GetEmployeeAttendance(request);
            return HandleResult(result);
        }

        
        [HttpPost("UpsertAttendance")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpsertAttendance([FromBody] PostAttendanceRequest request)
        {
            var result = await _attendanceService.UpsertAttendance(request);
            return HandleResult(result);
        }

        [HttpPost("UploadAttendances")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> InsertAttendances([FromBody] PostAttendancesRequest request)
        {
            var result = await _attendanceService.InsertAttendances(request);
            return HandleResult(result);
        }

        [HttpPost("UploadAttendancesTxt")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> HandleEncrypt([FromBody] EncryptTxtAttendanceRequest request)
        {
            var result = await _attendanceService.HandelEncryptData(request);
            return HandleResult(result);
        }


    }
}
