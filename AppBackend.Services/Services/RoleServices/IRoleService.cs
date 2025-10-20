using AppBackend.BusinessObjects.Dtos;
using AppBackend.Services.ApiModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppBackend.Services.Services.RoleServices
{
    public interface IRoleService
    {
        Task<ResultModel<List<RoleDto>>> GetAllRolesAsync();
        Task<ResultModel<RoleDto>> GetRoleByIdAsync(int id);
        Task<ResultModel<RoleDto>> GetRoleByCommonCodeAsync(string commonCode);
        Task<ResultModel<List<RoleDto>>> SearchRolesAsync(SearchRoleRequest request);
        Task<ResultModel<RoleDto>> CreateRoleAsync(CreateRoleRequest request, int createdBy);
        Task<ResultModel<RoleDto>> UpdateRoleAsync(int roleId, UpdateRoleRequest request, int updatedBy);
        Task<ResultModel> DeleteRoleAsync(int roleId);
        Task<ResultModel<List<RoleDto>>> GetRolesByAccountIdAsync(int accountId);
        Task<ResultModel> AssignRolesToAccountAsync(AssignRolesRequest request, int assignedBy);
    }
}

