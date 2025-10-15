using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.ApiModels;
using System.Security.Claims;
using AppBackend.Services.Services.AccountServices;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing user profile (View Profile, Edit Profile, Account Summary)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        /// <summary>
        /// View customer profile
        /// </summary>
        /// <returns>Customer profile details</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="404">Profile not found</response>
        [HttpGet("customer-profile")]
        [Authorize]
        public async Task<IActionResult> ViewCustomerProfile()
        {
            // Get current user's AccountId from claims
            int accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _accountService.GetCustomerProfileAsync(accountId);
            if (!result.IsSuccess)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Edit customer profile
        /// </summary>
        /// <param name="request">Profile data to update</param>
        /// <returns>Updated customer profile details</returns>
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Invalid input data</response>
        [HttpPut("customer-profile")]
        [Authorize]
        public async Task<IActionResult> EditCustomerProfile([FromBody] EditCustomerProfileRequest request)
        {
            // Get current user's AccountId from claims
            request.AccountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _accountService.EditCustomerProfileAsync(request);
            if (!result.IsSuccess)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Xem tổng quan tài khoản (Summary)
        /// </summary>
        /// <param name="accountId">ID tài khoản cần xem (tùy chọn, mặc định là tài khoản hiện tại)</param>
        /// <returns>Thông tin tổng quan tài khoản bao gồm: roles, thông tin chi tiết (Customer/Employee), và thống kê (nếu Admin xem)</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy tài khoản</response>
        /// <response code="403">Không có quyền xem tài khoản này</response>
        [HttpGet("summary")]
        [Authorize]
        public async Task<IActionResult> ViewAccountSummary([FromQuery] int? accountId = null)
        {
            // Get current user's AccountId
            int currentAccountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Nếu không truyền accountId, xem summary của chính mình
            int targetAccountId = accountId ?? currentAccountId;
            
            // Kiểm tra quyền: chỉ được xem summary của chính mình, trừ khi là Admin
            var currentUserRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isAdmin = currentUserRoles.Contains("Admin");
            
            if (!isAdmin && targetAccountId != currentAccountId)
            {
                return Forbid(); // 403 - Không có quyền xem tài khoản khác
            }
            
            // Truyền requesterId để service biết có cần lấy statistics không
            var result = await _accountService.GetAccountSummaryAsync(targetAccountId, currentAccountId);
            
            if (!result.IsSuccess)
                return NotFound(result);
            
            return Ok(result);
        }

        /// <summary>
        /// Xem tổng quan tài khoản theo ID (Admin only)
        /// </summary>
        /// <param name="id">ID tài khoản cần xem</param>
        /// <returns>Thông tin tổng quan tài khoản với đầy đủ statistics</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy tài khoản</response>
        [HttpGet("summary/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ViewAccountSummaryById(int id)
        {
            int currentAccountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _accountService.GetAccountSummaryAsync(id, currentAccountId);
            
            if (!result.IsSuccess)
                return NotFound(result);
            
            return Ok(result);
        }
    }
}
