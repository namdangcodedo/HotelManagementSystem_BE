using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.BankConfigRepo
{
    public interface IBankConfigRepository : IGenericRepository<BankConfig>
    {
        Task<BankConfig?> GetActiveBankConfigAsync();
        Task<IEnumerable<BankConfig>> GetAllBankConfigsAsync();
    }
}
