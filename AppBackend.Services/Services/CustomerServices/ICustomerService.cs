using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.CustomerModel;

namespace AppBackend.Services.Services.CustomerServices
{
    public interface ICustomerService
    {
        Task<ResultModel> GetCustomerListAsync(GetCustomerListRequest request);
        Task<ResultModel> GetCustomerDetailAsync(int customerId);
        Task<ResultModel> BanCustomerAsync(BanCustomerRequest request);
    }
}
