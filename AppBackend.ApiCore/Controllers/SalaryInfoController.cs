using AppBackend.Services.ApiModels.SalaryModel;
using AppBackend.Services.Services.SalaryInfoServices;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace AppBackend.ApiCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalaryInfoController : BaseApiController
    {
        private readonly ISalaryInfoService _service;

        public SalaryInfoController(ISalaryInfoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] GetSalaryInfoRequest request)
        {
            if (!ModelState.IsValid) return ValidationError("D? li?u không h?p l?");
            var result = await _service.GetAsync(request);
            return HandleResult(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return HandleResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PostSalaryInfoRequest request)
        {
            if (!ModelState.IsValid) return ValidationError("D? li?u không h?p l?");
            var result = await _service.CreateAsync(request);
            return HandleResult(result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PostSalaryInfoRequest request)
        {
            if (!ModelState.IsValid) return ValidationError("D? li?u không h?p l?");
            var result = await _service.UpdateAsync(id, request);
            return HandleResult(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return HandleResult(result);
        }

        [HttpPost("calculate")]

        public async Task<IActionResult> Calculate([FromBody] CalculateSalaryRequest request)
        {
            if (!ModelState.IsValid) return ValidationError("Dữ liệu không hợp lệ");

            var result = await _service.CalculateMonthlySalary(request);

            // If service returned failure, use existing handler
            if (!result.IsSuccess) return HandleResult(result);

            // Expect the service to return an object with FileBytes, ContentType, FileName
            if (result.Data != null)
            {
                var dataObj = result.Data;
                var type = dataObj.GetType();
                var fileBytesProp = type.GetProperty("FileBytes", BindingFlags.Public | BindingFlags.Instance);
                var contentTypeProp = type.GetProperty("ContentType", BindingFlags.Public | BindingFlags.Instance);
                var fileNameProp = type.GetProperty("FileName", BindingFlags.Public | BindingFlags.Instance);

                if (fileBytesProp != null && contentTypeProp != null && fileNameProp != null)
                {
                    var fileBytes = fileBytesProp.GetValue(dataObj) as byte[];
                    var contentType = contentTypeProp.GetValue(dataObj) as string ?? "application/octet-stream";
                    var fileName = fileNameProp.GetValue(dataObj) as string ?? "salary.xlsx";

                    if (fileBytes != null)
                    {
                        return File(fileBytes, contentType, fileName);
                    }
                }
            }

            // Fallback to default handler if file not present
            return HandleResult(result);
        }
    }
}