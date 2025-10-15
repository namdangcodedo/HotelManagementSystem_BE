using Microsoft.AspNetCore.Mvc;
using AppBackend.BusinessObjects.Dtos;
using System.Threading.Tasks;
using AppBackend.Services.Services.AmenityServices;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AmenityController : ControllerBase
    {
        private readonly IAmenityService _amenityService;
        public AmenityController(IAmenityService amenityService)
        {
            _amenityService = amenityService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddAmenity([FromBody] AmenityDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = 0;
            int.TryParse(userIdStr, out userId);
            var result = await _amenityService.AddAmenityAsync(dto, userId);
            return Ok(result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateAmenity([FromBody] AmenityDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = 0;
            int.TryParse(userIdStr, out userId);
            var result = await _amenityService.UpdateAmenityAsync(dto, userId);
            return Ok(result);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteAmenity(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = 0;
            int.TryParse(userIdStr, out userId);
            var result = await _amenityService.DeleteAmenityAsync(id, userId);
            return Ok(result);
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAmenityList([FromQuery] bool? isActive)
        {
            var result = await _amenityService.GetAmenityListAsync(isActive);
            return Ok(result);
        }

        [HttpPost("paged-list")]
        public async Task<IActionResult> GetAmenityPaged([FromBody] PagedAmenityRequestDto request)
        {
            var result = await _amenityService.GetAmenityPagedAsync(request);
            return Ok(result);
        }

        [HttpGet("detail/{id}")]
        public async Task<IActionResult> GetAmenityDetail(int id)
        {
            var result = await _amenityService.GetAmenityDetailAsync(id);
            return Ok(result);
        }
    }
}
