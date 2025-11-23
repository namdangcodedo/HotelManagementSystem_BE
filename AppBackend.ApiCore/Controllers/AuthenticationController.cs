using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.Authentication;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.AccountModel;
using AppBackend.Services.Helpers;
using Microsoft.AspNetCore.Authorization;
using AppBackend.BusinessObjects.AppSettings;
using Microsoft.Extensions.Options;
using System.Text.Json;
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
        /// Lấy URL để đăng nhập Google (cho Frontend)
        /// </summary>
        /// <param name="redirectUri">Redirect URI của frontend (optional, mặc định lấy từ config)</param>
        /// <returns>Google OAuth URL đầy đủ để redirect user</returns>
        /// <response code="200">Trả về Google login URL thành công</response>
        /// <remarks>
        /// API này trả về Google OAuth URL đầy đủ để Frontend redirect user.
        /// Frontend không cần biết Client ID, chỉ cần gọi API này và redirect.
        /// 
        /// === USAGE ===
        /// ```javascript
        /// // Frontend code
        /// const response = await fetch('http://localhost:8080/api/Authentication/google-login-url');
        /// const data = await response.json();
        /// 
        /// if (data.isSuccess) {
        ///   // Redirect user to Google
        ///   window.location.href = data.data.url;
        /// }
        /// ```
        /// 
        /// === RESPONSE ===
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "data": {
        ///     "url": "https://accounts.google.com/o/oauth2/v2/auth?client_id=...&redirect_uri=...&response_type=code&scope=openid%20email%20profile",
        ///     "redirectUri": "http://localhost:3000/auth/google/callback"
        ///   }
        /// }
        /// ```
        /// 
        /// === ADVANCED USAGE (Custom Redirect URI) ===
        /// Nếu muốn dùng redirect URI khác (ví dụ cho mobile app):
        /// ```
        /// GET /api/Authentication/google-login-url?redirectUri=myapp://auth/callback
        /// ```
        /// </remarks>
        [HttpGet("google-login-url")]
        public IActionResult GetGoogleLoginUrl([FromQuery] string? redirectUri = null)
        {
            try
            {
                // Sử dụng GoogleLoginService để tạo URL
                var googleLoginUrl = _googleLoginService.GetGoogleLoginUrl();
                
                // Lấy redirect URI từ service hoặc config
                var finalRedirectUri = redirectUri ?? _frontendSettings.BaseUrl + "/auth/google/callback";
                
                return Ok(new
                {
                    isSuccess = true,
                    data = new
                    {
                        url = googleLoginUrl,
                        redirectUri = finalRedirectUri,
                        // Thêm thông tin hữu ích cho frontend
                        scopes = new[] { "openid", "email", "profile" }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetGoogleLoginUrl] Error: {ex.Message}");
                return BadRequest(new
                {
                    isSuccess = false,
                    message = $"Không thể tạo Google login URL: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Exchange authorization code sent from frontend to backend (safe flow for SPA)
        /// Frontend should POST the `code` it received from Google to this endpoint.
        /// Backend will exchange the code (via IGoogleLoginService), create/login the user and
        /// return the app tokens. Backend will also set HttpOnly cookies if applicable.
        /// </summary>
        /// <param name="request">Object chứa authorization code và redirect URI từ Google</param>
        /// <returns>JWT tokens và thông tin user</returns>
        /// <response code="200">Exchange thành công, trả về token và user info</response>
        /// <response code="400">Code không hợp lệ hoặc đã hết hạn</response>
        /// <remarks>
        /// **LUỒNG 2 - Exchange Flow (Khuyến nghị cho SPA/Frontend)**
        /// 
        /// === FLOW ===
        /// 1. Frontend tự tạo Google Auth URL và redirect user đến Google
        /// 2. User đăng nhập Google
        /// 3. Google redirect về Frontend với code trong URL
        /// 4. Frontend POST code lên API này
        /// 5. Backend exchange code → access_token → user info → tạo JWT
        /// 6. Frontend nhận token và lưu vào localStorage/cookie
        /// 
        /// === FRONTEND EXAMPLE ===
        /// ```javascript
        /// // Step 1: Redirect to Google
        /// const googleAuthUrl = `https://accounts.google.com/o/oauth2/v2/auth?` +
        ///   `client_id=${CLIENT_ID}&` +
        ///   `redirect_uri=${encodeURIComponent('http://localhost:3000/auth/callback')}&` +
        ///   `response_type=code&` +
        ///   `scope=openid%20email%20profile`;
        /// window.location.href = googleAuthUrl;
        /// 
        /// // Step 2: In callback page, extract code from URL
        /// const params = new URLSearchParams(window.location.search);
        /// const code = params.get('code');
        /// 
        /// // Step 3: Send code to backend
        /// const response = await fetch('/api/Authentication/exchange-google', {
        ///   method: 'POST',
        ///   headers: { 'Content-Type': 'application/json' },
        ///   body: JSON.stringify({
        ///     code: code,
        ///     redirectUri: 'http://localhost:3000/auth/callback'
        ///   })
        /// });
        /// 
        /// const data = await response.json();
        /// if (data.isSuccess) {
        ///   localStorage.setItem('access_token', data.data.token);
        ///   localStorage.setItem('refresh_token', data.data.refreshToken);
        ///   // Redirect to dashboard
        /// }
        /// ```
        /// 
        /// === IMPORTANT NOTES ===
        /// - `redirectUri` trong request body PHẢI KHỚP CHÍNH XÁC với redirect_uri đã dùng khi lấy code từ Google
        /// - Authorization code chỉ sử dụng được 1 lần và hết hạn sau ~10 phút
        /// - Client secret được giữ bí mật trên backend, không bao giờ gửi về frontend
        /// - Nếu backend/frontend cùng domain, HttpOnly cookies sẽ được set tự động
        /// 
        /// === REQUEST ===
        /// POST /api/Authentication/exchange-google
        /// ```json
        /// {
        ///   "code": "4/0Ab32j906Ny14NCGN2Uc7kIdGKZHTLe...",
        ///   "redirectUri": "http://localhost:3000/auth/callback"
        /// }
        /// ```
        /// 
        /// === RESPONSE SUCCESS ===
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "data": {
        ///     "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ///     "refreshToken": "abc123def456...",
        ///     "user": {
        ///       "email": "user@gmail.com",
        ///       "name": "John Doe",
        ///       "picture": "https://lh3.googleusercontent.com/a/...",
        ///       "roles": ["Customer"]
        ///     }
        ///   }
        /// }
        /// ```
        /// 
        /// === RESPONSE ERROR ===
        /// ```json
        /// {
        ///   "isSuccess": false,
        ///   "message": "Code không hợp lệ hoặc đã được sử dụng"
        /// }
        /// ```
        /// 
        /// === TROUBLESHOOTING ===
        /// - **"redirect_uri_mismatch"**: redirectUri phải khớp chính xác với URI đã đăng ký trong Google Console
        /// - **"invalid_grant"**: Code đã được sử dụng hoặc hết hạn, cần lấy code mới
        /// - **"Code không hợp lệ"**: Code format sai hoặc không tồn tại
        /// </remarks>
        [HttpPost("exchange-google")]
        public async Task<IActionResult> ExchangeGoogle([FromBody] ExchangeGoogleRequest? request)
        {
            try
            {
                // Validate input
                if (request == null || string.IsNullOrWhiteSpace(request.Code))
                {
                    return BadRequest(new 
                    { 
                        isSuccess = false, 
                        message = "Code không hợp lệ. Vui lòng thử đăng nhập lại." 
                    });
                }

                // Log for debugging (remove in production or use proper logger)
                Console.WriteLine($"[GoogleLogin] Received code exchange request. Code length: {request.Code.Length}");

                // Exchange code for user info via GoogleLoginService
                // This service will:
                // 1. Call Google OAuth2 token endpoint with code + client_secret
                // 2. Get access_token from Google
                // 3. Call Google userinfo endpoint to get user details
                GoogleUserInfo userInfo;
                try
                {
                    userInfo = await _googleLoginService.GetUserInfoFromCodeAsync(request.Code);
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"[GoogleLogin] Google API error: {ex.Message}");
                    
                    // Parse error message for better user feedback
                    var errorMessage = "Không thể xác thực với Google. ";
                    if (ex.Message.Contains("invalid_grant"))
                        errorMessage += "Code đã được sử dụng hoặc hết hạn. Vui lòng thử đăng nhập lại.";
                    else if (ex.Message.Contains("redirect_uri_mismatch"))
                        errorMessage += "Cấu hình redirect URI không khớp. Vui lòng liên hệ quản trị viên.";
                    else
                        errorMessage += "Vui lòng thử lại sau.";
                    
                    return BadRequest(new { isSuccess = false, message = errorMessage });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GoogleLogin] Unexpected error during code exchange: {ex.Message}");
                    return BadRequest(new 
                    { 
                        isSuccess = false, 
                        message = $"Lỗi khi xử lý thông tin từ Google: {ex.Message}" 
                    });
                }

                if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
                {
                    return BadRequest(new 
                    { 
                        isSuccess = false, 
                        message = "Không thể lấy thông tin user từ Google. Vui lòng kiểm tra quyền truy cập email." 
                    });
                }

                Console.WriteLine($"[GoogleLogin] Successfully got user info from Google: {userInfo.Email}");

                // Login or create user in our system
                var result = await _authService.LoginWithGoogleCallbackAsync(userInfo);

                if (!result.IsSuccess)
                {
                    Console.WriteLine($"[GoogleLogin] Login failed: {result.Message}");
                    return HandleResult(result);
                }

                // Convert anonymous data to GoogleLoginResponse
                var jsonData = JsonSerializer.Serialize(result.Data);
                var loginResponse = JsonSerializer.Deserialize<GoogleLoginResponse>(jsonData, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
                {
                    Console.WriteLine("[GoogleLogin] Failed to generate JWT token");
                    return BadRequest(new 
                    { 
                        isSuccess = false, 
                        message = "Không thể tạo token đăng nhập. Vui lòng thử lại." 
                    });
                }

                Console.WriteLine($"[GoogleLogin] Successfully logged in user: {loginResponse.Email}");

                // Optionally set HttpOnly cookies for access/refresh tokens
                // This works if backend and frontend share the same domain
                try
                {
                    var accessCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true, // Requires HTTPS in production
                        SameSite = SameSiteMode.Lax, // Changed from Strict to Lax for better compatibility
                        Expires = DateTimeOffset.UtcNow.AddHours(1)
                    };

                    var refreshCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddDays(30)
                    };

                    Response.Cookies.Append("access_token", loginResponse.Token, accessCookieOptions);
                    if (!string.IsNullOrEmpty(loginResponse.RefreshToken))
                        Response.Cookies.Append("refresh_token", loginResponse.RefreshToken, refreshCookieOptions);
                    
                    Console.WriteLine("[GoogleLogin] HttpOnly cookies set successfully");
                }
                catch (Exception ex)
                {
                    // Don't fail the request if cookie setting fails
                    // Frontend can still use tokens from response body
                    Console.WriteLine($"[GoogleLogin] Warning: Could not set cookies: {ex.Message}");
                }

                // Return tokens and user info in response body
                // Frontend can use this when cookies are not viable (e.g., different domains)
                return Ok(new
                {
                    isSuccess = true,
                    message = "Đăng nhập Google thành công",
                    data = new
                    {
                        token = loginResponse.Token,
                        refreshToken = loginResponse.RefreshToken,
                        user = new 
                        {
                            email = loginResponse.Email,
                            name = loginResponse.Name,
                            picture = loginResponse.Picture,
                            roles = loginResponse.Roles
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleLogin] Unhandled exception: {ex.Message}\n{ex.StackTrace}");
                return BadRequest(new 
                { 
                    isSuccess = false, 
                    message = $"Lỗi không xác định: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// DTO for exchange-google endpoint
        /// </summary>
        public class ExchangeGoogleRequest
        {
            /// <summary>
            /// Authorization code nhận được từ Google OAuth2
            /// </summary>
            public string? Code { get; set; }
            
            /// <summary>
            /// Redirect URI đã sử dụng khi lấy code (phải khớp chính xác)
            /// </summary>
            public string? RedirectUri { get; set; }
            
            /// <summary>
            /// Code verifier cho PKCE flow (optional, dành cho public clients)
            /// </summary>
            public string? CodeVerifier { get; set; }
        }

        /// <summary>
        /// Xử lý callback từ Google sau khi đăng nhập (redirect flow)
        /// </summary>
        /// <param name="code">Authorization code từ Google</param>
        /// <param name="state">State parameter để verify CSRF (optional)</param>
        /// <returns>Redirect về frontend với access token và refresh token</returns>
        /// <response code="302">Redirect về frontend thành công</response>
        /// <response code="400">Mã xác thực không hợp lệ</response>
        /// <remarks>
        /// **LUỒNG 1 - Redirect Flow (cho server-side hoặc mobile app)**
        /// 
        /// === FLOW ===
        /// 1. Frontend gọi GET /login-google để lấy Google Auth URL
        /// 2. Frontend redirect user đến URL đó
        /// 3. User đăng nhập Google
        /// 4. **Google redirect TRỰC TIẾP về endpoint này** (Backend)
        /// 5. Backend tự động exchange code, tạo JWT, và redirect về Frontend với token trong URL
        /// 6. Frontend parse token từ URL query params
        /// 
        /// === GOOGLE CONSOLE CONFIG ===
        /// Authorized redirect URIs phải chứa:
        /// - http://localhost:8080/api/Authentication/callback-google (development)
        /// - https://your-api-domain.com/api/Authentication/callback-google (production)
        /// 
        /// === FRONTEND EXAMPLE ===
        /// ```javascript
        /// // Step 1: Get Google login URL
        /// const response = await fetch('/api/Authentication/login-google');
        /// const { url } = await response.json();
        /// 
        /// // Step 2: Redirect to Google
        /// window.location.href = url;
        /// 
        /// // Step 3: Create callback page to handle redirect from backend
        /// // URL will be: http://localhost:3000/auth/google/callback?token=...&refreshToken=...
        /// const params = new URLSearchParams(window.location.search);
        /// const token = params.get('token');
        /// const refreshToken = params.get('refreshToken');
        /// const error = params.get('error');
        /// 
        /// if (error) {
        ///   console.error('Login failed:', params.get('message'));
        /// } else if (token) {
        ///   localStorage.setItem('access_token', token);
        ///   localStorage.setItem('refresh_token', refreshToken);
        ///   window.location.href = '/dashboard';
        /// }
        /// ```
        /// 
        /// === SECURITY NOTE ===
        /// ⚠️ Token trong URL có thể bị lộ qua browser history/logs.
        /// Khuyến nghị dùng LUỒNG 2 (exchange-google) cho SPA để tránh vấn đề này.
        /// </remarks>
        [HttpGet("callback-google")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string? state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    Console.WriteLine("[GoogleCallback] No code provided");
                    var errorUrl = $"{_frontendSettings.BaseUrl}/auth/google/callback?error=invalid_code&message={Uri.EscapeDataString("Mã xác thực không hợp lệ")}";
                    return Redirect(errorUrl);
                }

                Console.WriteLine($"[GoogleCallback] Received callback with code length: {code.Length}");

                // Exchange code for user info
                GoogleUserInfo userInfo;
                try
                {
                    userInfo = await _googleLoginService.GetUserInfoFromCodeAsync(code);
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"[GoogleCallback] Google API error: {ex.Message}");
                    var errorMessage = ex.Message.Contains("invalid_grant") 
                        ? "Code đã được sử dụng hoặc hết hạn" 
                        : "Không thể xác thực với Google";
                    var errorUrl = $"{_frontendSettings.BaseUrl}/auth/google/callback?error=google_error&message={Uri.EscapeDataString(errorMessage)}";
                    return Redirect(errorUrl);
                }

                if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
                {
                    Console.WriteLine("[GoogleCallback] Invalid user info from Google");
                    var errorUrl = $"{_frontendSettings.BaseUrl}/auth/google/callback?error=invalid_user&message={Uri.EscapeDataString("Không thể lấy thông tin user từ Google")}";
                    return Redirect(errorUrl);
                }

                Console.WriteLine($"[GoogleCallback] Got user info: {userInfo.Email}");

                // Login or create user
                var result = await _authService.LoginWithGoogleCallbackAsync(userInfo);

                if (!result.IsSuccess)
                {
                    Console.WriteLine($"[GoogleCallback] Login failed: {result.Message}");
                    var errorUrl = $"{_frontendSettings.BaseUrl}/auth/google/callback?error=login_failed&message={Uri.EscapeDataString(result.Message ?? "Đăng nhập thất bại")}";
                    return Redirect(errorUrl);
                }

                // Serialize and deserialize to convert anonymous object to GoogleLoginResponse
                var jsonData = JsonSerializer.Serialize(result.Data);
                var loginResponse = JsonSerializer.Deserialize<GoogleLoginResponse>(jsonData, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
                {
                    Console.WriteLine("[GoogleCallback] Failed to generate token");
                    var errorUrl = $"{_frontendSettings.BaseUrl}/auth/google/callback?error=server_error&message={Uri.EscapeDataString("Không thể xử lý dữ liệu đăng nhập")}";
                    return Redirect(errorUrl);
                }

                Console.WriteLine($"[GoogleCallback] Login successful for: {loginResponse.Email}");

                // Try to set HttpOnly cookies before redirect
                try
                {
                    var accessCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddHours(1)
                    };

                    var refreshCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddDays(30)
                    };

                    Response.Cookies.Append("access_token", loginResponse.Token, accessCookieOptions);
                    if (!string.IsNullOrEmpty(loginResponse.RefreshToken))
                        Response.Cookies.Append("refresh_token", loginResponse.RefreshToken, refreshCookieOptions);
                    
                    Console.WriteLine("[GoogleCallback] Cookies set successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GoogleCallback] Could not set cookies: {ex.Message}");
                }

                // Redirect to frontend with tokens in URL (as fallback if cookies don't work)
                var successUrl = $"{_frontendSettings.BaseUrl}/auth/google/callback?token={Uri.EscapeDataString(loginResponse.Token)}&refreshToken={Uri.EscapeDataString(loginResponse.RefreshToken)}";
                return Redirect(successUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleCallback] Unhandled exception: {ex.Message}\n{ex.StackTrace}");
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

            if (string.IsNullOrWhiteSpace(request.Password))
                return ValidationError("Mật khẩu mới không được để trống");

            var result = await _authService.ResetPasswordAsync(request.Email, request.Password!);
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
