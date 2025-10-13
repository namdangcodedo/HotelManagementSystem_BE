using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.RoleRepo
{
    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<Role?> GetByNameAsync(string roleName);
    }
}