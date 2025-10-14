using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.ApiModels;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing user profile (View Profile, Edit Profile)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly AppBackend.Services.AccountServices.IAccountService _accountService;

        public AccountController(AppBackend.Services.AccountServices.IAccountService accountService)
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
    }
}
