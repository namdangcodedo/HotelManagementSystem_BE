using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.RoleRepo
{
    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<IEnumerable<Role>> GetAllRolesAsync();
        Task<Role?> GetRoleByIdAsync(int id);
        Task<Role?> GetRoleByRoleValueAsync(string roleName);
    }
}

