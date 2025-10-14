using AppBackend.Services.ApiModels;
using Microsoft.Extensions.Caching.Memory;

namespace AppBackend.Services.Authentication
{
    public interface IAuthenticationService
    {
        Task<ResultModel> RegisterAsync(RegisterRequest request);
        Task<ResultModel> LoginAsync(LoginRequest request);
        Task LogoutAsync(LogoutRequest request);
        Task<ResultModel> GoogleLoginAsync(GoogleLoginRequest request);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IMemoryCache _cache;
        public AuthenticationService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<ResultModel> RegisterAsync(RegisterRequest request)
        {
            // Implement registration logic and cache refresh token if needed
            return new ResultModel { IsSuccess = true, Message = "Register success" };
        }

        public async Task<ResultModel> LoginAsync(LoginRequest request)
        {
            // Implement login logic and cache refresh token
            _cache.Set("RefreshToken", "sample_token");
            return new ResultModel { IsSuccess = true, Message = "Login success" };
        }

        public async Task LogoutAsync(LogoutRequest request)
        {
            // Remove refresh token from cache
            _cache.Remove("RefreshToken");
        }

        public async Task<ResultModel> GoogleLoginAsync(GoogleLoginRequest request)
        {
            // Implement Google login logic and cache refresh token
            _cache.Set("RefreshToken", "google_token");
            return new ResultModel { IsSuccess = true, Message = "Google login success" };
        }
    }
}

