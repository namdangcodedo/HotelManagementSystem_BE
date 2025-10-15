using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.Authentication;
using AppBackend.Services.ApiModels;
using AppBackend.Services.Helpers;
using AppBackend.Services.Services.Email;
using Microsoft.AspNetCore.Authorization;
using LoginRequest = AppBackend.Services.ApiModels.LoginRequest;
using RegisterRequest = AppBackend.Services.ApiModels.RegisterRequest;
using ResetPasswordRequest = AppBackend.Services.ApiModels.ResetPasswordRequest;

namespace AppBackend.ApiCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IGoogleLoginService _googleLoginService;
        private readonly IEmailService _emailService;
        private readonly CacheHelper _cacheHelper;

        public AuthenticationController(IAuthenticationService authService, IGoogleLoginService googleLoginService, IEmailService emailService, CacheHelper cacheHelper)
        {
            _authService = authService;
            _googleLoginService = googleLoginService;
            _emailService = emailService;
            _cacheHelper = cacheHelper;
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
            var result = await _authService.LogoutAsync(request);
            return Ok(result);
        }

        [HttpGet("login-google")]
        public IActionResult LoginGoogle()
        {
            var googleLoginUrl = _googleLoginService.GetGoogleLoginUrl();
            return Ok(new { url = googleLoginUrl });
        }

        [HttpGet("callback-google")]
        public async Task<IActionResult> GoogleCallback(string code)
        {
            var userInfo = await _googleLoginService.GetUserInfoFromCodeAsync(code);
            var result = await _authService.LoginWithGoogleCallbackAsync(userInfo);
            return Ok(result);
        }
        
        ///// <summary>
        ///// Reset mật khẩu cho người dùng đã quên mật khẩu.
        ///// </summary>
        ///// <param name="request">Gửi email và reset với quyền Admin</param>
        ///// <returns>.</returns>
        //[HttpPost("send-verification-email")]
        [HttpPost("manager/reset-password")]
        // [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> SendOtpEmail([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request.Email, request.Password);
            return Ok(result);
        }
        
        /// <summary>
        /// Gửi mã OTP về email để xác thực quên mật khẩu.
        /// </summary>
        /// <param name="request">Thông tin email cần gửi OTP.</param>
        /// <returns>Kết quả gửi OTP.</returns>
        [HttpPost("send-otp-email")]
        public async Task<IActionResult> SendOtpEmail([FromBody] SendOtpRequest request)
        {
            var result = await _authService.SendOtpAsync(request.Email);
            return Ok(result);
        }

        /// <summary>
        /// Kiểm tra mã OTP nhập vào có hợp lệ không (quên mật khẩu).
        /// </summary>
        /// <param name="request">Thông tin email và mã OTP cần kiểm tra.</param>
        /// <returns>Kết quả kiểm tra OTP.</returns>
        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var result = _authService.VerifyOtp(request.Email, request.Otp);
            return Ok(result);
        }

        /// <summary>
        /// Đổi mật khẩu mới khi xác thực OTP thành công (quên mật khẩu).
        /// </summary>
        /// <param name="request">Thông tin email, mã OTP và mật khẩu mới.</param>
        /// <returns>Kết quả đổi mật khẩu.</returns>
        [HttpPost("change-password-with-otp")]
        public async Task<IActionResult> ChangePasswordWithOtp([FromBody] ChangePasswordWithOtpRequest request)
        {
            var result = await _authService.ChangePasswordWithOtpAsync(request.Email, request.Otp, request.NewPassword);
            return Ok(result);
        }

        [HttpPost("get-token")]
        public async Task<IActionResult> GetToken([FromBody] GetTokenRequest request)
        {
            var result = await _authService.GetTokenAsync(request.AccountId, request.RefreshToken);
            return Ok(result);
        }

        /// <summary>
        /// Lấy refresh token từ cache theo accountId (chỉ cho phép lấy của chính mình)
        /// </summary>
        [HttpGet("refresh-token")]
        [Authorize]
        public IActionResult GetRefreshToken()
        {
            var userClaimId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userClaimId))
                return Unauthorized();
            var refreshToken = _cacheHelper.Get<string>(CachePrefix.RefreshToken, userClaimId);
            if (refreshToken == null)
                return NotFound(new { Message = "Refresh token không tồn tại hoặc đã hết hạn." });
            return Ok(new { RefreshToken = refreshToken });
        }
    }
}
