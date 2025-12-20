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
                IsLocked = true, // ✅ Đặt IsLocked = true khi đăng ký
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null,
                UpdatedAt = null,
                UpdatedBy = null
            };
            await _unitOfWork.Accounts.AddAsync(account);
            await _unitOfWork.SaveChangesAsync();
            
            if (account.AccountId == 0)
                return new ResultModel { IsSuccess = false, Message = "Không thể tạo Account, AccountId không hợp lệ." };

            // Create Customer linked to Account
            var customer = new Customer
            {
                AccountId = account.AccountId,
                FullName = request.FullName,
                IdentityCard = request.IdentityCard,
                Address = request.Address,
                PhoneNumber = request.PhoneNumber,
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

            // ✅ Lưu activation token vào cache với thời gian hết hạn 5 phút
            var activationToken = Guid.NewGuid().ToString();
            _cacheHelper.Set(CachePrefix.AccountActivation, account.AccountId.ToString(), activationToken, TimeSpan.FromMinutes(5));

            // ✅ Gửi email kích hoạt
            try
            {
                await _emailService.SendAccountActivationEmailAsync(account.AccountId);
            }
            catch (Exception emailEx)
            {
                Console.WriteLine($"Failed to send activation email: {emailEx.Message}");
            }

            return new ResultModel
            {
                IsSuccess = true,
                Message = "Đăng ký thành công! Vui lòng kiểm tra email để kích hoạt tài khoản trong vòng 5 phút.",
                Data = new
                {
                    AccountId = account.AccountId,
                    Email = account.Email,
                    Message = "Email kích hoạt đã được gửi"
                }
            };
        }

        public async Task<ResultModel> LoginAsync(LoginRequest request)
        {
            // Tìm account bằng username hoặc email
            var account = await _unitOfWork.Accounts.GetByUsernameOrEmailAsync(request.Email);
            
            if (account == null || !_accountHelper.VerifyPassword(request.Password, account.PasswordHash))
                return new ResultModel { IsSuccess = false, Message = "Sai tài khoản hoặc mật khẩu" };
            
            if (account.IsLocked)
            {
                return new ResultModel { IsSuccess = false, Message = "Tài khoản đã bị khoá" };
            }
            
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
                // ✅ Account chưa tồn tại - Tạo mới
                Console.WriteLine($"[GoogleLogin] Creating new account for email: {userInfo.Email}");
                
                var randomPassword = new Random().Next(100000, 999999).ToString();
                var hashedPassword = _accountHelper.HashPassword(randomPassword);
                account = new Account
                {
                    Username = userInfo.Email,
                    Email = userInfo.Email,
                    PasswordHash = hashedPassword,
                    IsLocked = false,
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Accounts.AddAsync(account);
                await _unitOfWork.SaveChangesAsync();
                
                Console.WriteLine($"[GoogleLogin] Account created with ID: {account.AccountId}");
                
                // Tạo Customer
                var customer = new Customer
                {
                    AccountId = account.AccountId,
                    FullName = userInfo.Name,
                    Address = "",
                    IdentityCard = "",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Customers.AddAsync(customer);
                await _unitOfWork.SaveChangesAsync();
                
                Console.WriteLine($"[GoogleLogin] Customer created for account: {account.AccountId}");

                // Gán User role
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
                    
                    Console.WriteLine($"[GoogleLogin] User role assigned to account: {account.AccountId}");
                }
            }
            else
            {
                // ✅ Account đã tồn tại - Không tạo mới
                Console.WriteLine($"[GoogleLogin] Account already exists for email: {userInfo.Email} (ID: {account.AccountId})");
                
                // Kiểm tra xem có Customer chưa, nếu chưa thì tạo
                var existingCustomer = (await _unitOfWork.Customers.FindAsync(c => c.AccountId == account.AccountId)).FirstOrDefault();
                if (existingCustomer == null)
                {
                    Console.WriteLine($"[GoogleLogin] Customer not found, creating Customer for existing account: {account.AccountId}");
                    var customer = new Customer
                    {
                        AccountId = account.AccountId,
                        FullName = userInfo.Name,
                        Address = "",
                        IdentityCard = "",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Customers.AddAsync(customer);
                    await _unitOfWork.SaveChangesAsync();
                }
                
                // Kiểm tra xem có role chưa, nếu chưa thì gán
                var existingRoles = await _unitOfWork.Accounts.GetRoleNamesByAccountIdAsync(account.AccountId);
                if (existingRoles == null || !existingRoles.Any())
                {
                    Console.WriteLine($"[GoogleLogin] No roles found, assigning User role to account: {account.AccountId}");
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
            }

            // Update last login time
            account.LastLoginAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);
            await _unitOfWork.SaveChangesAsync();

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
            // Validate email
            if (string.IsNullOrWhiteSpace(email))
                return new ResultModel { IsSuccess = false, Message = "Email không được để trống." };
            
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
            // Validate password
            if (string.IsNullOrWhiteSpace(newPassword))
                return new ResultModel { IsSuccess = false, Message = "Mật khẩu không được để trống." };
            
            if (newPassword.Length < 8 || newPassword.Length > 50)
                return new ResultModel { IsSuccess = false, Message = "Mật khẩu phải có độ dài từ 8 đến 50 ký tự." };
            
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

        public async Task<ResultModel> GetTokenAsync(int accountId, string refreshToken)
        {
            var cachedRefreshToken = _cacheHelper.Get<string>(CachePrefix.RefreshToken, accountId.ToString());
            if (string.IsNullOrEmpty(cachedRefreshToken) || cachedRefreshToken != refreshToken)
                return new ResultModel { IsSuccess = false, Message = "Refresh token không hợp lệ hoặc đã hết hạn." };
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
            if (account == null)
                return new ResultModel { IsSuccess = false, Message = "Tài khoản không tồn tại." };
            var roleNames = await _unitOfWork.Accounts.GetRoleNamesByAccountIdAsync(account.AccountId);
            var accessToken = _accountHelper.CreateToken(account, roleNames);
            return new ResultModel { IsSuccess = true, Message = "Lấy access token thành công.", Data = new { AccessToken = accessToken } };
        }

        public async Task<ResultModel> ActivateAccountAsync(string token)
        {
            try
            {
                // 1. Decode token để lấy accountId (sử dụng AccountTokenHelper)
                var accountTokenHelper = new AccountTokenHelper(_configuration);
                var accountId = accountTokenHelper.DecodeAccountToken(token);

                // 2. Kiểm tra xem token có tồn tại trong cache không (đã hết hạn 5 phút chưa)
                var cachedToken = _cacheHelper.Get<string>(CachePrefix.AccountActivation, accountId.ToString());
                if (cachedToken == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = "Link kích hoạt đã hết hạn (quá 5 phút). Vui lòng gửi lại email kích hoạt.",
                        StatusCode = 400
                    };
                }

                // 3. Lấy account từ database
                var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
                if (account == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = "Tài khoản không tồn tại.",
                        StatusCode = 404
                    };
                }

                // 4. Kiểm tra xem account đã được kích hoạt chưa
                if (!account.IsLocked)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = "Tài khoản đã được kích hoạt trước đó. Bạn có thể đăng nhập ngay.",
                        StatusCode = 400
                    };
                }

                // 5. Kích hoạt account (set IsLocked = false)
                account.IsLocked = false;
                account.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Accounts.UpdateAsync(account);
                await _unitOfWork.SaveChangesAsync();

                // 6. Xóa token khỏi cache
                _cacheHelper.Remove(CachePrefix.AccountActivation, accountId.ToString());

                // 7. ✅ Tạo access token và refresh token như Login
                var roleNames = await _unitOfWork.Accounts.GetRoleNamesByAccountIdAsync(account.AccountId);
                var accessToken = _accountHelper.CreateToken(account, roleNames);
                var refreshToken = _accountHelper.GenerateRefreshToken();
                
                // 8. ✅ Lưu refresh token vào cache
                _cacheHelper.Set(CachePrefix.RefreshToken, account.AccountId.ToString(), refreshToken);

                return new ResultModel
                {
                    IsSuccess = true,
                    Message = "Kích hoạt tài khoản thành công! Đang tự động đăng nhập...",
                    Data = new
                    {
                        Email = account.Email,
                        Username = account.Username,
                        Token = accessToken,
                        RefreshToken = refreshToken,
                        Roles = roleNames
                    },
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = $"Token không hợp lệ: {ex.Message}",
                    StatusCode = 400
                };
            }
        }

        public async Task<ResultModel> ResendActivationEmailAsync(string email)
        {
            try
            {
                // 1. Tìm account theo email
                var account = await _unitOfWork.Accounts.GetByEmailAsync(email);
                if (account == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = "Email không tồn tại trong hệ thống.",
                        StatusCode = 404
                    };
                }

                // 2. Kiểm tra xem account đã được kích hoạt chưa
                if (!account.IsLocked)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = "Tài khoản đã được kích hoạt trước đó. Bạn có thể đăng nhập ngay.",
                        StatusCode = 400
                    };
                }

                // 3. Tạo token mới và lưu vào cache với thời gian hết hạn 5 phút
                var activationToken = Guid.NewGuid().ToString();
                _cacheHelper.Set(CachePrefix.AccountActivation, account.AccountId.ToString(), activationToken, TimeSpan.FromMinutes(5));

                // 4. Gửi email kích hoạt
                try
                {
                    await _emailService.SendAccountActivationEmailAsync(account.AccountId);
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"Failed to send activation email: {emailEx.Message}");
                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = "Không thể gửi email kích hoạt. Vui lòng thử lại sau.",
                        StatusCode = 500
                    };
                }

                return new ResultModel
                {
                    IsSuccess = true,
                    Message = "Email kích hoạt đã được gửi lại! Vui lòng kiểm tra email và kích hoạt trong vòng 5 phút.",
                    Data = new
                    {
                        Email = account.Email,
                        Message = "Link kích hoạt mới có hiệu lực trong 5 phút"
                    },
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = $"Lỗi hệ thống: {ex.Message}",
                    StatusCode = 500
                };
            }
        }
    }
}

