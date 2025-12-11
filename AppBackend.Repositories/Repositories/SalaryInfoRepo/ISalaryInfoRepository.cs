using AppBackend.BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.SalaryInfoRepo
{
    public interface ISalaryInfoRepository
    {
        Task<List<SalaryInfo>> GetAllAsync();
        Task<SalaryInfo?> GetByIdAsync(int id);
        Task<List<SalaryInfo>> GetByEmployeeIdAsync(int employeeId);
        Task AddAsync(SalaryInfo entity);
        Task UpdateAsync(SalaryInfo entity);
        Task DeleteAsync(SalaryInfo entity);
    }
}