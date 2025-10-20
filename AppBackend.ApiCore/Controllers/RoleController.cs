using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.Services.Services.RoleServices;
using System.Threading.Tasks;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing roles and assigning roles to accounts
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoleController : BaseApiController
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        /// <summary>
        /// Get all active roles
        /// </summary>
        /// <returns>List of all active roles</returns>
        /// <response code="200">Roles retrieved successfully</response>
        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            var result = await _roleService.GetAllRolesAsync();
            return HandleResult(result);
        }

        /// <summary>
        /// Get role by ID
        /// </summary>
        /// <param name="id">Role ID</param>
        /// <returns>Role details</returns>
        /// <response code="200">Role found</response>
        /// <response code="404">Role not found</response>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleById(int id)
        {
            var result = await _roleService.GetRoleByIdAsync(id);
            return HandleResult(result);
        }

        /// <summary>
        /// Get role by CommonCode (searches in CommonCode table with CodeType = "ROLE")
        /// </summary>
        /// <param name="commonCode">Common code value (e.g., "ADMIN", "MANAGER")</param>
        /// <returns>Role details</returns>
        /// <response code="200">Role found</response>
        /// <response code="404">Role not found</response>
        [HttpGet("by-commoncode/{commonCode}")]
        public async Task<IActionResult> GetRoleByCommonCode(string commonCode)
        {
            var result = await _roleService.GetRoleByCommonCodeAsync(commonCode);
            return HandleResult(result);
        }

        /// <summary>
        /// Search roles by name, value, or description
        /// </summary>
        /// <param name="request">Search criteria</param>
        /// <returns>List of matching roles</returns>
        /// <response code="200">Search completed successfully</response>
        [HttpGet("search")]
        public async Task<IActionResult> SearchRoles([FromQuery] SearchRoleRequest request)
        {
            var result = await _roleService.SearchRolesAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Create a new role (Admin only)
        /// </summary>
        /// <param name="request">Role creation data</param>
        /// <returns>Created role details</returns>
        /// <response code="201">Role created successfully</response>
        /// <response code="409">RoleValue already exists</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _roleService.CreateRoleAsync(request, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// Update role information (Admin only)
        /// </summary>
        /// <param name="id">Role ID</param>
        /// <param name="request">Update data</param>
        /// <returns>Updated role details</returns>
        /// <response code="200">Role updated successfully</response>
        /// <response code="404">Role not found</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _roleService.UpdateRoleAsync(id, request, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// Delete a role (Admin only)
        /// </summary>
        /// <param name="id">Role ID</param>
        /// <returns>Success message</returns>
        /// <response code="200">Role deleted successfully</response>
        /// <response code="404">Role not found</response>
        /// <response code="409">Cannot delete - role is in use</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var result = await _roleService.DeleteRoleAsync(id);
            return HandleResult(result);
        }

        /// <summary>
        /// Get all roles assigned to an account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>List of roles assigned to the account</returns>
        /// <response code="200">Roles retrieved successfully</response>
        /// <response code="404">Account not found</response>
        [HttpGet("account/{accountId}")]
        public async Task<IActionResult> GetRolesByAccountId(int accountId)
        {
            var result = await _roleService.GetRolesByAccountIdAsync(accountId);
            return HandleResult(result);
        }

        /// <summary>
        /// Assign multiple roles to an account (Admin/Manager only)
        /// </summary>
        /// <param name="request">Account ID and list of Role IDs</param>
        /// <returns>Success message</returns>
        /// <response code="200">Roles assigned successfully</response>
        /// <response code="404">Account not found</response>
        /// <response code="400">Invalid role IDs</response>
        [HttpPost("assign")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AssignRolesToAccount([FromBody] AssignRolesRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _roleService.AssignRolesToAccountAsync(request, CurrentUserId);
            return HandleResult(result);
        }
    }
}

