using AppBackend.BusinessObjects.Dtos;
using AppBackend.Services.ApiModels;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AppBackend.Services.Services.Role
{
    public interface IRoleService
    {
        Task<ResultModel> GetAllRolesAsync();
        Task<ResultModel> GetRoleByIdAsync(int id);
        Task<ResultModel> CreateRoleAsync(RoleDto dto);
        Task<ResultModel> UpdateRoleAsync(int id, RoleDto dto);
        Task<ResultModel> DeleteRoleAsync(int id);
    }
}

