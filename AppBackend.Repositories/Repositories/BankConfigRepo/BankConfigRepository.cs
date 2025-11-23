using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.BankConfigRepo
{
    public class BankConfigRepository : GenericRepository<BankConfig>, IBankConfigRepository
    {
        private readonly HotelManagementContext _context;

        public BankConfigRepository(HotelManagementContext context) : base(context)
        {
            _context = context;
        }

        public async Task<BankConfig?> GetActiveBankConfigAsync()
        {
            return await _context.BankConfigs
                .Where(bc => bc.IsActive)
                .OrderByDescending(bc => bc.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<BankConfig>> GetAllBankConfigsAsync()
        {
            return await _context.BankConfigs
                .OrderByDescending(bc => bc.IsActive)
                .ThenByDescending(bc => bc.CreatedAt)
                .ToListAsync();
        }
    }
}
