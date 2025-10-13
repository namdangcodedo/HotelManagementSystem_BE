using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.RoleRepo
{
    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        private readonly IOTShowroomContext _context;

        public RoleRepository(IOTShowroomContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Role?> GetByNameAsync(string roleName)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == roleName);
        }
    }
}