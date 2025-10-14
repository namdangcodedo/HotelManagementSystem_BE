using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.ApiModels;
using AppBackend.Services.AccountServices;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing user profile (View Profile, Edit Profile)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        /// <summary>
        /// View user profile
        /// </summary>
        /// <returns>User profile details</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="404">Profile not found</response>
        [HttpGet("profile")]
        public async Task<IActionResult> ViewProfile()
        {
            // Assume userId is retrieved from claims or session
            int userId = 1; // Replace with actual logic
            var result = await _accountService.GetAccountByIdAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Edit user profile
        /// </summary>
        /// <param name="request">Profile data to update</param>
        /// <returns>Updated profile details</returns>
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Invalid input data</response>
        [HttpPut("profile")]
        public async Task<IActionResult> EditProfile([FromBody] EditProfileRequest request)
        {
            var result = await _accountService.EditProfileAsync(request);
            return Ok(result);
        }
    }
}
