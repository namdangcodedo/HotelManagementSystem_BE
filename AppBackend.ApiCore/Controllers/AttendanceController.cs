using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using AppBackend.Services.Services.AttendanceServices;
using AppBackend.Services.ApiModels.AttendanceModel;
using AppBackend.Services.ApiModels;

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

        [HttpGet("")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetEmployeeAttendance([FromQuery] GetAttendanceRequest request)
        {
            var result = await _attendanceService.GetEmployeeAttendance(request);
            return HandleResult(result);
        }

        
        [HttpPost("")]
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

        /// <summary>
        /// Upload an encrypted attendance text file.
        /// The endpoint now accepts a single file (multipart/form-data).
        /// File content can be:
        ///  - a JSON matching EncryptTxtAttendanceRequest { EncryptTxt, Iv }
        ///  - or plain text where first non-empty line is EncryptTxt and the second line (optional) is Iv.
        /// </summary>
        [HttpPost("UploadAttendancesTxt")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> HandleEncrypt([FromForm(Name = "file")] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ResultModel
                {
                    IsSuccess = false,
                    Message = "File is required",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            string content;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                content = await reader.ReadToEndAsync();
            }

            var result = await _attendanceService.HandelTxtData(content);
            return HandleResult(result);
        }

        [HttpGet("static-info")]
        public async Task<IActionResult> GetStaticInfo([FromQuery] GetAttendanceRequest request)
        {
            var result = await _attendanceService.GetStaticInfo(request);
            return HandleResult(result);
        }


    }
}
