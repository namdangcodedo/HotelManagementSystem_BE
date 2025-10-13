using AppBackend.BusinessObjects.Dtos;
using AppBackend.Services.Services.Role;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AppBackend.ApiCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _roleService.GetAllRolesAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _roleService.GetRoleByIdAsync(id);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoleDto dto)
        {
            var result = await _roleService.CreateRoleAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = ((RoleDto)result.Data).RoleId }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RoleDto dto)
        {
            var result = await _roleService.UpdateRoleAsync(id, dto);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _roleService.DeleteRoleAsync(id);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }
    }
}

