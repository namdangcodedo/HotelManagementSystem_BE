using AppBackend.Services;
using AppBackend.Services.ApiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Attributes;

namespace AppBackend.Api.Controllers
{
    /// <summary>
    /// APIs for managing users (Register, Login, Query Users)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="request">Registration request payload</param>
        /// <returns>JWT access token and refresh token</returns>
        /// <response code="201">User registered successfully</response>
        /// <response code="400">Invalid input data</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [RateLimit(permitLimit: 3, windowSeconds: 60, queueLimit: 1, strategy: "fixed")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.RegisterAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Login an existing user
        /// </summary>
        /// <param name="request">Login credentials (Email + Password)</param>
        /// <returns>JWT access token and refresh token</returns>
        /// <response code="200">Login successful</response>
        /// <response code="401">Invalid credentials</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [RateLimit(permitLimit: 5, windowSeconds: 60, queueLimit: 2, strategy: "sliding")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.LoginAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get all users (only for Admin or Manager)
        /// </summary>
        /// <returns>List of users</returns>
        /// <response code="200">Users retrieved successfully</response>
        /// <response code="403">Forbidden (not enough role permissions)</response>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        [RateLimit(5, 30)] 
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get user detail by Id
        /// </summary>
        /// <param name="id">User Id</param>
        /// <returns>User detail</returns>
        /// <response code="200">User retrieved successfully</response>
        /// <response code="404">User not found</response>
        [HttpGet("{id:int}")]
        [Authorize]
        [RateLimit(permitLimit: 10, windowSeconds: 30, strategy: "token")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }
    }
}
