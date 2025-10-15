using AppBackend.BusinessObjects.Enums;
using AppBackend.Services.ApiModels;
using AppBackend.Services.Helpers;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.BusinessObjects.Models;
using AppBackend.Services.Services.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace AppBackend.Services.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly AccountHelper _accountHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly CacheHelper _cacheHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthenticationService(AccountHelper accountHelper, IUnitOfWork unitOfWork, CacheHelper cacheHelper, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IEmailService emailService)
        {
            _accountHelper = accountHelper;
            _unitOfWork = unitOfWork;
            _cacheHelper = cacheHelper;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<ResultModel> RegisterAsync(RegisterRequest request)
        {
            var existing = await _unitOfWork.Accounts.GetByEmailAsync(request.Email);
            if (existing != null)
                return new ResultModel { IsSuccess = false, Message = "Email đã tồn tại" };

            var userRole = await _unitOfWork.Roles.GetRoleByRoleValueAsync(RoleEnums.User.ToString());
            var hashedPassword = _accountHelper.HashPassword(request.Password);
            var account = new Account
            {
                Username = request.Username,
                PasswordHash = hashedPassword,
                Email = request.Email,
                IsLocked = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null,
                UpdatedAt = null,
                UpdatedBy = null
            };
            await _unitOfWork.Accounts.AddAsync(account);
            await _unitOfWork.SaveChangesAsync();

            // Create Customer linked to Account
            var customer = new Customer
            {
                AccountId = account.AccountId,
                FullName = request.FullName,
                IdentityCard = request.IdentityCard,
                Address = request.Address,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null,
                UpdatedAt = null,
                UpdatedBy = null
            };
            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            // Assign User role
            if (userRole != null)
            {
                var accountRole = new AccountRole
                {
                    AccountId = account.AccountId,
                    RoleId = userRole.RoleId
                };
                await _unitOfWork.Accounts.AddAccountRoleAsync(accountRole);
                await _unitOfWork.SaveChangesAsync();
            }

            var refreshToken = _accountHelper.GenerateRefreshToken();
            // Lưu refresh token vào cache
            _cacheHelper.Set(CachePrefix.RefreshToken, account.AccountId.ToString(), refreshToken);
            var roleNames = await _unitOfWork.Accounts.GetRoleNamesByAccountIdAsync(account.AccountId);
            var token = _accountHelper.CreateToken(account, roleNames);
            return new ResultModel
            {
                IsSuccess = true,
                Message = "Đăng ký thành công",
                Data = new
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Roles = roleNames
                }
            };
        }

        public async Task<ResultModel> LoginAsync(LoginRequest request)
        {
            var account = await _unitOfWork.Accounts.GetByEmailAsync(request.Email);
            if (account == null || !_accountHelper.VerifyPassword(request.Password, account.PasswordHash))
                return new ResultModel { IsSuccess = false, Message = "Sai tài khoản hoặc mật khẩu" };

            // Update lastLoginAt on successful login
            account.LastLoginAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);
            await _unitOfWork.SaveChangesAsync();

            var roleNames = await _unitOfWork.Accounts.GetRoleNamesByAccountIdAsync(account.AccountId);
            var token = _accountHelper.CreateToken(account, roleNames);
            var refreshToken = _accountHelper.GenerateRefreshToken();
            // Lưu refresh token vào cache
            _cacheHelper.Set(CachePrefix.RefreshToken, account.AccountId.ToString(), refreshToken);
            return new ResultModel {
                IsSuccess = true,
                Message = "Đăng nhập thành công",
                Data = new {
                    Token = token,
                    RefreshToken = refreshToken,
                    Roles = roleNames
                }
            };
        }

        public async Task<ResultModel> LogoutAsync(LogoutRequest request)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userClaimId = httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userClaimId == null || userClaimId != request.AccountId.ToString())
            {
                return new ResultModel {
                    IsSuccess = false,
                    Message = "Bạn không có quyền đăng xuất tài khoản này"
                };
            }
            _cacheHelper.Remove(CachePrefix.RefreshToken, request.AccountId.ToString());
            return new ResultModel {
                IsSuccess = true,
                Message = "Đăng xuất thành công"
            };
        }

        public async Task<ResultModel> GoogleLoginAsync(GoogleLoginRequest request)
        {
            // 1. Lấy Google Client ID từ cấu hình
            var googleClientId = _configuration["GoogleAuth:ClientId"];
            Google.Apis.Auth.GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { googleClientId }
                };
                payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch
            {
                return new ResultModel { IsSuccess = false, Message = "Token Google không hợp lệ" };
            }

            // 2. Find or create account by email from Google payload
            var account = await _unitOfWork.Accounts.GetByEmailAsync(payload.Email);
            if (account == null)
            {
                account = new Account
                {
                    Username = payload.Email,
                    Email = payload.Email,
                    IsLocked = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Accounts.AddAsync(account);
                await _unitOfWork.SaveChangesAsync();

                // Assign default role
                var userRole = await _unitOfWork.Roles.GetRoleByRoleValueAsync("User");
                if (userRole != null)
                {
                    var accountRole = new AccountRole
                    {
                        AccountId = account.AccountId,
                        RoleId = userRole.RoleId
                    };
                    await _unitOfWork.Accounts.AddAccountRoleAsync(accountRole);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            // 3. Generate tokens
            var roleNames = await _unitOfWork.Accounts.GetRoleNamesByAccountIdAsync(account.AccountId);
            var token = _accountHelper.CreateToken(account, roleNames);
            var refreshToken = _accountHelper.GenerateRefreshToken();
            _cacheHelper.Set(CachePrefix.RefreshToken, account.AccountId.ToString(), refreshToken);

            // 4. Return result
            return new ResultModel
            {
                IsSuccess = true,
                Message = "Đăng nhập Google thành công",
                Data = new
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Roles = roleNames
                }
            };
        }

        public async Task<ResultModel> LoginWithGoogleCallbackAsync(GoogleUserInfo userInfo)
        {
            var account = await _unitOfWork.Accounts.GetByEmailAsync(userInfo.Email);
            if (account == null)
            {
                var randomPassword = new Random().Next(100000, 999999).ToString();
                var hashedPassword = _accountHelper.HashPassword(randomPassword);
                account = new Account
                {
                    Username = userInfo.Email,
                    Email = userInfo.Email,
                    PasswordHash = hashedPassword,
                    IsLocked = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Accounts.AddAsync(account);
                await _unitOfWork.SaveChangesAsync();

                var userRole = await _unitOfWork.Roles.GetRoleByRoleValueAsync(RoleEnums.User.ToString());
                if (userRole != null)
                {
                    var accountRole = new AccountRole
                    {
                        AccountId = account.AccountId,
                        RoleId = userRole.RoleId
                    };
                    await _unitOfWork.Accounts.AddAccountRoleAsync(accountRole);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            var roleNames = await _unitOfWork.Accounts.GetRoleNamesByAccountIdAsync(account.AccountId);
            var token = _accountHelper.CreateToken(account, roleNames);
            var refreshToken = _accountHelper.GenerateRefreshToken();
            _cacheHelper.Set(CachePrefix.RefreshToken, account.AccountId.ToString(), refreshToken);

            return new ResultModel
            {
                IsSuccess = true,
                Message = "Đăng nhập Google thành công",
                Data = new
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Roles = roleNames,
                    Email = userInfo.Email,
                    Name = userInfo.Name,
                    Picture = userInfo.Picture
                }
            };
        }

        public async Task<ResultModel> ResetPasswordAsync(string email, string newPassword)
        {
            var account = await _unitOfWork.Accounts.GetByEmailAsync(email);
            if (account == null)
                return new ResultModel { IsSuccess = false, Message = "Tài khoản không tồn tại." };

            string passwordToSet;
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                var random = new Random();
                passwordToSet = random.Next(100000, 999999).ToString();
            }
            else
            {
                passwordToSet = newPassword;
            }
            
            await _emailService.SendEmail(email, "Mật khẩu mới của bạn", $"Mật khẩu mới của bạn là: <b>{passwordToSet}</b>");

            var hashedPassword = _accountHelper.HashPassword(passwordToSet);
            account.PasswordHash = hashedPassword;
            account.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);
            await _unitOfWork.SaveChangesAsync();

            string message = string.IsNullOrWhiteSpace(newPassword)
                ? "Mật khẩu mới đã được tạo ngẫu nhiên và gửi về email của bạn."
                : "Đổi mật khẩu thành công.";
            return new ResultModel { IsSuccess = true, Message = message };
        }

        public async Task<ResultModel> SendOtpAsync(string email)
        {
            var account = await _unitOfWork.Accounts.GetByEmailAsync(email);
            if (account == null)
                return new ResultModel { IsSuccess = false, Message = "Tài khoản không tồn tại." };
            var random = new Random();
            var otp = random.Next(100000, 999999).ToString();
            await _emailService.SendOtpEmail(email, otp);
            _cacheHelper.Set(CachePrefix.OtpCode, email, otp, TimeSpan.FromSeconds(60));
            return new ResultModel { IsSuccess = true, Message = "OTP đã được gửi về email." };
        }

        public ResultModel VerifyOtp(string email, string otp)
        {
            var cachedOtp = _cacheHelper.Get<string>(CachePrefix.OtpCode, email);
            if (cachedOtp == null)
                return new ResultModel { IsSuccess = false, Message = "OTP đã hết hạn hoặc không tồn tại." };
            if (cachedOtp != otp)
                return new ResultModel { IsSuccess = false, Message = "OTP không đúng." };
            return new ResultModel { IsSuccess = true, Message = "OTP hợp lệ." };
        }

        public async Task<ResultModel> ChangePasswordWithOtpAsync(string email, string otp, string newPassword)
        {
            var cachedOtp = _cacheHelper.Get<string>(CachePrefix.OtpCode, email);
            if (cachedOtp == null)
                return new ResultModel { IsSuccess = false, Message = "OTP đã hết hạn hoặc không tồn tại." };
            if (cachedOtp != otp)
                return new ResultModel { IsSuccess = false, Message = "OTP không đúng." };
            var account = await _unitOfWork.Accounts.GetByEmailAsync(email);
            if (account == null)
                return new ResultModel { IsSuccess = false, Message = "Tài khoản không tồn tại." };
            var hashedPassword = _accountHelper.HashPassword(newPassword);
            account.PasswordHash = hashedPassword;
            account.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);
            await _unitOfWork.SaveChangesAsync();
            _cacheHelper.Remove(CachePrefix.OtpCode, email);
            return new ResultModel { IsSuccess = true, Message = "Đổi mật khẩu thành công." };
        }
    }
}