using AppBackend.BusinessObjects.Dtos;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.RoleRepo;
using AppBackend.Services.ApiModels;
using AutoMapper;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AppBackend.Services.Services.Role
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;

        public RoleService(IRoleRepository roleRepository, IMapper mapper)
        {
            _roleRepository = roleRepository;
            _mapper = mapper;
        }

        public async Task<ResultModel> GetAllRolesAsync()
        {
            var roles = await _roleRepository.GetAllRolesAsync();
            var dtos = _mapper.Map<IEnumerable<RoleDto>>(roles);
            return new ResultModel { IsSuccess = true, Data = dtos };
        }

        public async Task<ResultModel> GetRoleByIdAsync(int id)
        {
            var role = await _roleRepository.GetRoleByIdAsync(id);
            if (role == null)
                return new ResultModel { IsSuccess = false, Message = "Role not found" };
            var dto = _mapper.Map<RoleDto>(role);
            return new ResultModel { IsSuccess = true, Data = dto };
        }

        public async Task<ResultModel> CreateRoleAsync(RoleDto dto)
        {
            var role = _mapper.Map<Role>(dto);
            await _roleRepository.AddRoleAsync(role);
            return new ResultModel { IsSuccess = true, Data = _mapper.Map<RoleDto>(role) };
        }

        public async Task<ResultModel> UpdateRoleAsync(int id, RoleDto dto)
        {
            var role = await _roleRepository.GetRoleByIdAsync(id);
            if (role == null)
                return new ResultModel { IsSuccess = false, Message = "Role not found" };
            _mapper.Map(dto, role);
            await _roleRepository.UpdateRoleAsync(role);
            return new ResultModel { IsSuccess = true, Data = _mapper.Map<RoleDto>(role) };
        }

        public async Task<ResultModel> DeleteRoleAsync(int id)
        {
            var role = await _roleRepository.GetRoleByIdAsync(id);
            if (role == null)
                return new ResultModel { IsSuccess = false, Message = "Role not found" };
            await _roleRepository.DeleteRoleAsync(role);
            return new ResultModel { IsSuccess = true };
        }
    }
}
