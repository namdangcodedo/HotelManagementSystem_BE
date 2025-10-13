using AppBackend.Services.ApiModels;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AppBackend.Services.Services.Account
{
    public interface IAccountService
    {
        Task<ResultModel> RegisterAsync(RegisterRequest request);
        Task<ResultModel> LoginAsync(LoginRequest request);
        Task<ResultModel> GetAllAccountsAsync();
        Task<ResultModel> GetAccountByIdAsync(int id);
    }
}