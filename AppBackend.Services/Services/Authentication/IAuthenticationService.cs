using AppBackend.Services.ApiModels;

namespace AppBackend.Services.Authentication;

public interface IAuthenticationService
{
    Task<ResultModel> RegisterAsync(RegisterRequest request);
    Task<ResultModel> LoginAsync(LoginRequest request);
    Task<ResultModel> LogoutAsync(LogoutRequest request);
    Task<ResultModel> LoginWithGoogleCallbackAsync(GoogleUserInfo userInfo);
    Task<ResultModel> SendOtpAsync(string email);
    ResultModel VerifyOtp(string email, string otp);
    Task<ResultModel> ChangePasswordWithOtpAsync(string email, string otp, string newPassword);
    Task<ResultModel> ResetPasswordAsync(string email, string newPassword);
}
