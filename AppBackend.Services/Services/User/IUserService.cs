using AppBackend.Services.ApiModels;
using LoginRequest = AppBackend.Services.ApiModels.LoginRequest;
using RegisterRequest = AppBackend.Services.ApiModels.RegisterRequest;

namespace AppBackend.Services
{
    public interface IUserService
    {
        Task<ResultModel> RegisterAsync(RegisterRequest request);
        Task<ResultModel> LoginAsync(LoginRequest request);
        Task<ResultModel> GetAllUsersAsync();
        Task<ResultModel> GetUserByIdAsync(int id);
    }
}