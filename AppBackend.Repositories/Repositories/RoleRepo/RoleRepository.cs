using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
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
            return await GetAllAsync();
        }

        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            return await GetByIdAsync(id);
        }

        public async Task<Role?> GetRoleByRoleValueAsync(string roleName)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.RoleValue == roleName);
        }
    }
}

