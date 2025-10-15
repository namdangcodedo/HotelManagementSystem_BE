using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.EmployeeRepo
{
    public class EmployeeRepository : GenericRepository<Employee>, IEmployeeRepository
    {
        private readonly HotelManagementContext _context;

        public EmployeeRepository(HotelManagementContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Employee?> GetEmployeeByAccountIdAsync(int accountId)
        {
            return await _context.Employees
                .Include(e => e.Account)
                .Include(e => e.EmployeeType)
                .FirstOrDefaultAsync(e => e.AccountId == accountId);
        }

        public async Task<Employee?> GetEmployeeWithAccountAsync(int employeeId)
        {
            return await _context.Employees
                .Include(e => e.Account)
                .Include(e => e.EmployeeType)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
        }
    }
}

