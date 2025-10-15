using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.Authentication;
using AppBackend.Services.ApiModels;
using AppBackend.Services.Helpers;
using Microsoft.AspNetCore.Authorization;
using LoginRequest = AppBackend.Services.ApiModels.LoginRequest;
using RegisterRequest = AppBackend.Services.ApiModels.RegisterRequest;
using ResetPasswordRequest = AppBackend.Services.ApiModels.ResetPasswordRequest;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for authentication and user management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : BaseApiController
    {
        private readonly IAuthenticationService _authService;
        private readonly IGoogleLoginService _googleLoginService;
        private readonly CacheHelper _cacheHelper;

        public AuthenticationController(
            IAuthenticationService authService, 
            IGoogleLoginService googleLoginService, 
            CacheHelper cacheHelper)
        {
            _authService = authService;
            _googleLoginService = googleLoginService;
            _cacheHelper = cacheHelper;
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        /// <param name="request">Thông tin đăng ký</param>
        /// <returns>Kết quả đăng ký</returns>
        /// <response code="200">Đăng ký thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ hoặc email đã tồn tại</response>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _authService.RegisterAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Đăng nhập vào hệ thống
        /// </summary>
        /// <param name="request">Thông tin đăng nhập</param>
        /// <returns>Access token và refresh token</returns>
        /// <response code="200">Đăng nhập thành công</response>
        /// <response code="401">Thông tin đăng nhập không chính xác</response>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _authService.LoginAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Đăng xuất khỏi hệ thống
        /// </summary>
        /// <param name="request">Thông tin đăng xuất</param>
        /// <returns>Kết quả đăng xuất</returns>
        /// <response code="200">Đăng xuất thành công</response>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var result = await _authService.LogoutAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Lấy URL để đăng nhập bằng Google
        /// </summary>
        /// <returns>URL đăng nhập Google</returns>
        /// <response code="200">Lấy URL thành công</response>
        [HttpGet("login-google")]
        public IActionResult LoginGoogle()
        {
            var googleLoginUrl = _googleLoginService.GetGoogleLoginUrl();
            return Ok(new { url = googleLoginUrl });
        }

        /// <summary>
        /// Xử lý callback từ Google sau khi đăng nhập
        /// </summary>
        /// <param name="code">Authorization code từ Google</param>
        /// <returns>Access token và refresh token</returns>
        /// <response code="200">Đăng nhập Google thành công</response>
        /// <response code="400">Mã xác thực không hợp lệ</response>
        [HttpGet("callback-google")]
        public async Task<IActionResult> GoogleCallback(string code)
        {
            if (string.IsNullOrEmpty(code))
                return ValidationError("Mã xác thực không hợp lệ");

            var userInfo = await _googleLoginService.GetUserInfoFromCodeAsync(code);
            var result = await _authService.LoginWithGoogleCallbackAsync(userInfo);
            return HandleResult(result);
        }
        
        /// <summary>
        /// Reset mật khẩu cho người dùng (Admin/Manager only)
        /// </summary>
        /// <param name="request">Email và mật khẩu mới</param>
        /// <returns>Kết quả reset mật khẩu</returns>
        /// <response code="200">Reset mật khẩu thành công</response>
        /// <response code="404">Không tìm thấy tài khoản</response>
        [HttpPost("manager/reset-password")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ResetPasswordByManager([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _authService.ResetPasswordAsync(request.Email, request.Password);
            return HandleResult(result);
        }
        
        /// <summary>
        /// Gửi mã OTP về email để xác thực quên mật khẩu
        /// </summary>
        /// <param name="request">Thông tin email cần gửi OTP</param>
        /// <returns>Kết quả gửi OTP</returns>
        /// <response code="200">Gửi OTP thành công</response>
        /// <response code="404">Không tìm thấy tài khoản</response>
        [HttpPost("send-otp-email")]
        public async Task<IActionResult> SendOtpEmail([FromBody] SendOtpRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _authService.SendOtpAsync(request.Email);
            return HandleResult(result);
        }

        /// <summary>
        /// Kiểm tra mã OTP nhập vào có hợp lệ không (quên mật khẩu)
        /// </summary>
        /// <param name="request">Thông tin email và mã OTP cần kiểm tra</param>
        /// <returns>Kết quả kiểm tra OTP</returns>
        /// <response code="200">OTP hợp lệ</response>
        /// <response code="400">OTP không hợp lệ hoặc đã hết hạn</response>
        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = _authService.VerifyOtp(request.Email, request.Otp);
            return HandleResult(result);
        }

        /// <summary>
        /// Đổi mật khẩu mới khi xác thực OTP thành công (quên mật khẩu)
        /// </summary>
        /// <param name="request">Thông tin email, mã OTP và mật khẩu mới</param>
        /// <returns>Kết quả đổi mật khẩu</returns>
        /// <response code="200">Đổi mật khẩu thành công</response>
        /// <response code="400">OTP không hợp lệ hoặc đã hết hạn</response>
        [HttpPost("change-password-with-otp")]
        public async Task<IActionResult> ChangePasswordWithOtp([FromBody] ChangePasswordWithOtpRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _authService.ChangePasswordWithOtpAsync(request.Email, request.Otp, request.NewPassword);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy access token mới bằng refresh token
        /// </summary>
        /// <param name="request">Account ID và refresh token</param>
        /// <returns>Access token mới</returns>
        /// <response code="200">Lấy token thành công</response>
        /// <response code="401">Refresh token không hợp lệ hoặc đã hết hạn</response>
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] GetTokenRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _authService.GetTokenAsync(request.AccountId, request.RefreshToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy refresh token từ cache theo accountId (chỉ cho phép lấy của chính mình)
        /// </summary>
        /// <returns>Refresh token hiện tại</returns>
        /// <response code="200">Lấy refresh token thành công</response>
        /// <response code="404">Refresh token không tồn tại hoặc đã hết hạn</response>
        [HttpGet("refresh-token")]
        [Authorize]
        public IActionResult GetRefreshToken()
        {
            var userClaimId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userClaimId))
                return Unauthorized(new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "UNAUTHORIZED",
                    StatusCode = 401,
                    Message = "Không tìm thấy thông tin người dùng"
                });

            var refreshToken = _cacheHelper.Get<string>(CachePrefix.RefreshToken, userClaimId);
            
            if (refreshToken == null)
                return NotFound(new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "NOT_FOUND",
                    StatusCode = 404,
                    Message = "Refresh token không tồn tại hoặc đã hết hạn"
                });

            return Ok(new ResultModel
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = new { RefreshToken = refreshToken },
                Message = "Lấy refresh token thành công"
            });
        }
    }
}
