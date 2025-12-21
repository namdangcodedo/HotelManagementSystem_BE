using AppBackend.BusinessObjects.Models;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.SalaryModel;

namespace AppBackend.Services.Services.SalaryInfoServices
{
    public interface ISalaryInfoService
    {
        Task<ResultModel> GetAsync(GetSalaryInfoRequest request);
        Task<ResultModel> GetByIdAsync(int id);
        Task<ResultModel> CreateAsync(PostSalaryInfoRequest request);
        Task<ResultModel> UpdateAsync(int id, PostSalaryInfoRequest request);
        Task<ResultModel> DeleteAsync(int id);
        Task<ResultModel> CalculateMonthlySalary(CalculateSalaryRequest request);

    }
}