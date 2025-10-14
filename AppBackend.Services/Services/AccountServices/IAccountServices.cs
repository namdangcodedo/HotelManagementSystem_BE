using AppBackend.Services.ApiModels;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.Data;
using RegisterRequest = AppBackend.Services.ApiModels.RegisterRequest;

namespace AppBackend.Services.AccountServices
{
    public interface IAccountService
    {
        Task<ResultModel> RegisterAsync(RegisterRequest request);
        Task<ResultModel> LoginAsync(LoginRequest request);
        Task<ResultModel> GetAllAccountsAsync();
        Task<ResultModel> GetAccountByIdAsync(int id);
    }
}