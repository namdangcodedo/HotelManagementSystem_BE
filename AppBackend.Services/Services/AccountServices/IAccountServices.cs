using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.AccountModel;

namespace AppBackend.Services.Services.AccountServices;

public interface IAccountService
{
    Task<ResultModel> GetCustomerProfileAsync(int accountId);
    Task<ResultModel> EditCustomerProfileAsync(EditCustomerProfileRequest request);
    Task<ResultModel> GetAccountSummaryAsync(int accountId, int? requesterId = null);
}