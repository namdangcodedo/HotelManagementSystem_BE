using AppBackend.Services.Authentication;
using AppBackend.Services.ApiModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AppBackend.ApiCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authService;

        public AuthenticationController(IAuthenticationService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            await _authService.LogoutAsync(request);
            return Ok();
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var result = await _authService.GoogleLoginAsync(request);
            return Ok(result);
        }
    }
}

