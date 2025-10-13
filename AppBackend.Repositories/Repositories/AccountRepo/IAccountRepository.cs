using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.AccountRepo
{
    public interface IAccountRepository : IGenericRepository<Account>
    {
        Task<Account?> GetByEmailAsync(string email);
        // Thêm các phương thức đặc thù nếu cần
    }
}

