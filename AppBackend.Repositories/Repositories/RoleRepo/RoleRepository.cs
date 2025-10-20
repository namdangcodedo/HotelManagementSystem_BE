using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.RoleRepo
{
    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        private readonly HotelManagementContext _context;

        public RoleRepository(HotelManagementContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            return await _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.RoleName)
                .ToListAsync();
        }

        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            return await _context.Roles
                .Include(r => r.AccountRoles)
                .FirstOrDefaultAsync(r => r.RoleId == id);
        }

        public async Task<Role?> GetRoleByRoleValueAsync(string roleValue)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleValue == roleValue);
        }

        public async Task<Role?> GetRoleByCommonCodeAsync(string commonCode)
        {
            // Tìm CommonCode với CodeType = "ROLE"
            var commonCodeEntity = await _context.CommonCodes
                .FirstOrDefaultAsync(cc => cc.CodeType == "ROLE" && cc.CodeValue == commonCode && cc.IsActive);

            if (commonCodeEntity == null)
                return null;

            // Tìm Role theo RoleValue trùng với CodeValue
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleValue == commonCodeEntity.CodeValue && r.IsActive);
        }

        public async Task<IEnumerable<Role>> SearchRolesAsync(string? searchTerm, bool? isActive)
        {
            var query = _context.Roles.AsQueryable();

            // Lọc theo search term (tìm trong RoleName, RoleValue, Description)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(r => 
                    r.RoleName.ToLower().Contains(searchTerm) ||
                    r.RoleValue.ToLower().Contains(searchTerm) ||
                    (r.Description != null && r.Description.ToLower().Contains(searchTerm))
                );
            }

            // Lọc theo trạng thái
            if (isActive.HasValue)
            {
                query = query.Where(r => r.IsActive == isActive.Value);
            }

            return await query.OrderBy(r => r.RoleName).ToListAsync();
        }

        public async Task<IEnumerable<Role>> GetRolesByAccountIdAsync(int accountId)
        {
            return await _context.AccountRoles
                .Where(ar => ar.AccountId == accountId)
                .Include(ar => ar.Role)
                .Select(ar => ar.Role)
                .ToListAsync();
        }

        public async Task AddAccountRoleAsync(int accountId, int roleId)
        {
            var existingRole = await _context.AccountRoles
                .FirstOrDefaultAsync(ar => ar.AccountId == accountId && ar.RoleId == roleId);

            if (existingRole == null)
            {
                await _context.AccountRoles.AddAsync(new AccountRole
                {
                    AccountId = accountId,
                    RoleId = roleId
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveAccountRoleAsync(int accountId, int roleId)
        {
            var accountRole = await _context.AccountRoles
                .FirstOrDefaultAsync(ar => ar.AccountId == accountId && ar.RoleId == roleId);

            if (accountRole != null)
            {
                _context.AccountRoles.Remove(accountRole);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveAllAccountRolesAsync(int accountId)
        {
            var accountRoles = await _context.AccountRoles
                .Where(ar => ar.AccountId == accountId)
                .ToListAsync();

            _context.AccountRoles.RemoveRange(accountRoles);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsRoleAssignedToAccountAsync(int accountId, int roleId)
        {
            return await _context.AccountRoles
                .AnyAsync(ar => ar.AccountId == accountId && ar.RoleId == roleId);
        }
    }
}
