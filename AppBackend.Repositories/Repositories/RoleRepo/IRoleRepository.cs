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
        Task<Role?> GetRoleByRoleValueAsync(string roleValue);
        Task<Role?> GetRoleByCommonCodeAsync(string commonCode);
        Task<IEnumerable<Role>> SearchRolesAsync(string? searchTerm, bool? isActive);
        Task<IEnumerable<Role>> GetRolesByAccountIdAsync(int accountId);
        Task AddAccountRoleAsync(int accountId, int roleId);
        Task RemoveAccountRoleAsync(int accountId, int roleId);
        Task RemoveAllAccountRolesAsync(int accountId);
        Task<bool> IsRoleAssignedToAccountAsync(int accountId, int roleId);
    }
}
