using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
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
            return await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Email == email);
        }

        public async Task<List<string>> GetRoleNamesByAccountIdAsync(int accountId)
        {
            return await _context.Accounts
                .AsNoTracking()
                .Where(a => a.AccountId == accountId)
                .SelectMany(a => a.AccountRoles)
                .Select(ar => ar.Role.RoleValue)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<Role>> GetRolesByAccountIdAsync(int accountId)
        {
            return await _context.Accounts
                .AsNoTracking()
                .Where(a => a.AccountId == accountId)
                .SelectMany(a => a.AccountRoles)
                .Select(ar => ar.Role)
                .Distinct()
                .ToListAsync();
        }

        public async Task AddAccountRoleAsync(AccountRole accountRole)
        {
            await _context.AccountRoles.AddAsync(accountRole);
            await _context.SaveChangesAsync();
        }
    }
}
