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

        public async Task<IEnumerable<Employee>> SearchEmployeesAsync(string? keyword, int? employeeTypeId, bool? isActive, bool? isLocked)
        {
            var query = _context.Employees
                .Include(e => e.Account)
                .Include(e => e.EmployeeType)
                .AsQueryable();

            // Tìm kiếm theo keyword trên tất cả các trường
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(e =>
                    e.FullName.ToLower().Contains(keyword) ||
                    (e.PhoneNumber != null && e.PhoneNumber.ToLower().Contains(keyword)) ||
                    e.Account.Email.ToLower().Contains(keyword) ||
                    e.Account.Username.ToLower().Contains(keyword) ||
                    e.EmployeeType.CodeValue.ToLower().Contains(keyword)
                );
            }

            // Lọc theo loại nhân viên
            if (employeeTypeId.HasValue)
            {
                query = query.Where(e => e.EmployeeTypeId == employeeTypeId.Value);
            }

            // Lọc theo trạng thái hoạt động
            if (isActive.HasValue)
            {
                if (isActive.Value)
                {
                    query = query.Where(e => e.TerminationDate == null);
                }
                else
                {
                    query = query.Where(e => e.TerminationDate != null);
                }
            }

            // Lọc theo trạng thái tài khoản
            if (isLocked.HasValue)
            {
                query = query.Where(e => e.Account.IsLocked == isLocked.Value);
            }

            return await query
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
    }
}

