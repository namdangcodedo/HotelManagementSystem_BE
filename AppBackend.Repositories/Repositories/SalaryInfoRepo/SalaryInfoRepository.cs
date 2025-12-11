using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.SalaryInfoRepo
{
    public class SalaryInfoRepository : ISalaryInfoRepository
    {
        private readonly HotelManagementContext _context;

        public SalaryInfoRepository(HotelManagementContext context)
        {
            _context = context;
        }

        public async Task AddAsync(SalaryInfo entity)
        {
            await _context.Set<SalaryInfo>().AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(SalaryInfo entity)
        {
            _context.Set<SalaryInfo>().Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<SalaryInfo>> GetAllAsync()
        {
            return await _context.SalaryInfos.Include(s => s.Employee).ToListAsync();
        }

        public async Task<SalaryInfo?> GetByIdAsync(int id)
        {
            return await _context.Set<SalaryInfo>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SalaryInfoId == id);
        }

        public async Task<List<SalaryInfo>> GetByEmployeeIdAsync(int employeeId)
        {
            return await _context.Set<SalaryInfo>()
                .AsNoTracking()
                .Where(s => s.EmployeeId == employeeId)
                .ToListAsync();
        }

        public async Task UpdateAsync(SalaryInfo entity)
        {
            _context.Set<SalaryInfo>().Update(entity);
            await _context.SaveChangesAsync();
        }
    }
}