using System.Security.Claims;
using AppBackend.Services.ApiModels;

namespace AppBackend.Services.AccountServices;

public interface IAccountService
{
    Task<ResultModel> GetCustomerProfileAsync(int accountId);
    Task<ResultModel> EditCustomerProfileAsync(EditCustomerProfileRequest request);
}