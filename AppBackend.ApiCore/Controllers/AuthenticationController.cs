using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.Authentication;
using AppBackend.Services.ApiModels;
using AppBackend.Services.Helpers;
using Microsoft.AspNetCore.Authorization;
using AppBackend.BusinessObjects.AppSettings;
using Microsoft.Extensions.Options;
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
        private readonly FrontendSettings _frontendSettings;

        public AuthenticationController(
            IAuthenticationService authService, 
            IGoogleLoginService googleLoginService, 
            CacheHelper cacheHelper,
            IOptions<FrontendSettings> frontendSettings)
        {
            _authService = authService;
            _googleLoginService = googleLoginService;
            _cacheHelper = cacheHelper;
            _frontendSettings = frontendSettings.Value;
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
        /// <returns>Redirect về frontend với access token và refresh token</returns>
        /// <response code="302">Redirect về frontend thành công</response>
        /// <response code="400">Mã xác thực không hợp lệ</response>
        [HttpGet("callback-google")]
        public async Task<IActionResult> GoogleCallback(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    // Redirect về frontend với error
                    var errorUrl = $"{_frontendSettings.BaseUrl}/auth/google/callback?error=invalid_code&message=Mã xác thực không hợp lệ";
                    return Redirect(errorUrl);
                }

                var userInfo = await _googleLoginService.GetUserInfoFromCodeAsync(code);
                var result = await _authService.LoginWithGoogleCallbackAsync(userInfo);

                if (!result.IsSuccess)
                {
                    // Redirect về frontend với error
                    var errorUrl = $"{_frontendSettings.BaseUrl}/auth/google/callback?error=login_failed&message={Uri.EscapeDataString(result.Message)}";
                    return Redirect(errorUrl);
                }

                // Lấy token và refreshToken từ result.Data
                var data = result.Data as dynamic;
                var token = data?.Token?.ToString() ?? "";
                var refreshToken = data?.RefreshToken?.ToString() ?? "";

                // Redirect về frontend với token và refreshToken
                var successUrl = $"{_frontendSettings.BaseUrl}/auth/google/callback?token={Uri.EscapeDataString(token)}&refreshToken={Uri.EscapeDataString(refreshToken)}";
                return Redirect(successUrl);
            }
            catch (Exception ex)
            {
                // Redirect về frontend với error
                var errorUrl = $"{_frontendSettings.BaseUrl}/auth/google/callback?error=server_error&message={Uri.EscapeDataString(ex.Message)}";
                return Redirect(errorUrl);
            }
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
        [HttpGet("get-refresh-token/{accountId}")]
        [Authorize]
        public IActionResult GetRefreshToken(int accountId)
        {
            var refreshToken = _cacheHelper.Get<string>(CachePrefix.RefreshToken, accountId.ToString());
            if (string.IsNullOrEmpty(refreshToken))
                return NotFound(new ResultModel { IsSuccess = false, Message = "Refresh token không tồn tại hoặc đã hết hạn" });
            
            return Ok(new ResultModel { IsSuccess = true, Data = new { RefreshToken = refreshToken } });
        }

        /// <summary>
        /// Kích hoạt tài khoản bằng token từ email (không cần đăng nhập)
        /// </summary>
        /// <param name="token">Token mã hóa từ email kích hoạt</param>
        /// <returns>Kết quả kích hoạt tài khoản</returns>
        /// <response code="200">Kích hoạt thành công</response>
        /// <response code="400">Token không hợp lệ hoặc đã hết hạn (quá 5 phút)</response>
        /// <response code="404">Tài khoản không tồn tại</response>
        /// <remarks>
        /// API này cho phép user kích hoạt tài khoản từ link trong email mà không cần đăng nhập.
        /// Token được mã hóa 2 chiều từ accountId để bảo mật.
        /// 
        /// === USAGE ===
        /// - User nhận email với link: http://localhost:3000/activate-account/{token}
        /// - Frontend gọi API này với token từ URL
        /// - API decode token để lấy accountId và kích hoạt tài khoản
        /// - Token chỉ có hiệu lực trong 5 phút
        /// 
        /// === EXAMPLE ===
        /// GET /api/Authentication/activate-account/abc123xyz456
        /// 
        /// Response Success:
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "message": "Kích hoạt tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.",
        ///   "data": {
        ///     "email": "user@example.com",
        ///     "username": "user123"
        ///   }
        /// }
        /// ```
        /// 
        /// Response Error (Expired):
        /// ```json
        /// {
        ///   "isSuccess": false,
        ///   "message": "Link kích hoạt đã hết hạn (quá 5 phút). Vui lòng đăng ký lại."
        /// }
        /// ```
        /// </remarks>
        [HttpGet("activate-account/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> ActivateAccount(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ValidationError("Token không hợp lệ");

            var result = await _authService.ActivateAccountAsync(token);
            return HandleResult(result);
        }

        /// <summary>
        /// Gửi lại email kích hoạt tài khoản (không cần đăng nhập)
        /// </summary>
        /// <param name="request">Email cần gửi lại link kích hoạt</param>
        /// <returns>Kết quả gửi email</returns>
        /// <response code="200">Gửi email thành công</response>
        /// <response code="400">Tài khoản đã được kích hoạt</response>
        /// <response code="404">Email không tồn tại</response>
        /// <remarks>
        /// API này cho phép user yêu cầu gửi lại email kích hoạt nếu:
        /// - Link cũ đã hết hạn (quá 5 phút)
        /// - Không nhận được email lần đầu
        /// - Email bị mất hoặc xóa nhầm
        /// 
        /// === USAGE ===
        /// - User nhập email đã đăng ký
        /// - Frontend gọi API này
        /// - Hệ thống tạo token mới và gửi email kích hoạt
        /// - Token mới có hiệu lực 5 phút
        /// 
        /// === EXAMPLE ===
        /// POST /api/Authentication/resend-activation-email
        /// Body:
        /// ```json
        /// {
        ///   "email": "user@example.com"
        /// }
        /// ```
        /// 
        /// Response Success:
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "message": "Email kích hoạt đã được gửi lại! Vui lòng kiểm tra email và kích hoạt trong vòng 5 phút.",
        ///   "data": {
        ///     "email": "user@example.com",
        ///     "message": "Link kích hoạt mới có hiệu lực trong 5 phút"
        ///   }
        /// }
        /// ```
        /// 
        /// Response Error (Already Activated):
        /// ```json
        /// {
        ///   "isSuccess": false,
        ///   "message": "Tài khoản đã được kích hoạt trước đó. Bạn có thể đăng nhập ngay."
        /// }
        /// ```
        /// </remarks>
        [HttpPost("resend-activation-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendActivationEmail([FromBody] SendOtpRequest request)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Email))
                return ValidationError("Email không hợp lệ");

            var result = await _authService.ResendActivationEmailAsync(request.Email);
            return HandleResult(result);
        }
    }
}
