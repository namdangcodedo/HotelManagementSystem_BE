using AppBackend.Services.ApiModels.SalaryModel;
using AppBackend.Services.Services.SalaryInfoServices;
using Microsoft.AspNetCore.Mvc;

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
    }
}