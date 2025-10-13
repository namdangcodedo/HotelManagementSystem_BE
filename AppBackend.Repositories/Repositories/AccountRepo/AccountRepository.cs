using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.AccountRepo
{
    public class AccountRepository : GenericRepository<Account>, IAccountRepository
    {
        private readonly HotelManagementContext _context;

        public AccountRepository(HotelManagementContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Account?> GetByEmailAsync(string email)
        {
            return await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);
        }
        // Thêm các phương thức đặc thù nếu cần
    }
}

