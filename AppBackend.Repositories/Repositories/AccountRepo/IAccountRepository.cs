using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.AccountRepo
{
    public interface IAccountRepository : IGenericRepository<Account>
    {
        Task<Account?> GetByEmailAsync(string email);
        Task<List<string>> GetRoleNamesByAccountIdAsync(int accountId);
        Task<List<Role>> GetRolesByAccountIdAsync(int accountId);
    }
}
