using AppBackend.BusinessObjects.Dtos;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.AccountRepo;
using AppBackend.Repositories.Repositories.RoleRepo;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBackend.Services.Services.RoleServices
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RoleService(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ResultModel<List<RoleDto>>> GetAllRolesAsync()
        {
            var roles = await _unitOfWork.Roles.GetAllRolesAsync();
            var roleDtos = _mapper.Map<List<RoleDto>>(roles);

            return new ResultModel<List<RoleDto>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = roleDtos,
                Message = "Lấy danh sách role thành công"
            };
        }

        public async Task<ResultModel<RoleDto>> GetRoleByIdAsync(int id)
        {
            var role = await _unitOfWork.Roles.GetRoleByIdAsync(id);
            
            if (role == null)
            {
                return new ResultModel<RoleDto>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ResponseCode = "NOT_FOUND",
                    Message = "Không tìm thấy role"
                };
            }

            var roleDto = _mapper.Map<RoleDto>(role);

            return new ResultModel<RoleDto>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = roleDto,
                Message = "Lấy thông tin role thành công"
            };
        }

        public async Task<ResultModel<RoleDto>> GetRoleByCommonCodeAsync(string commonCode)
        {
            var role = await _unitOfWork.Roles.GetRoleByCommonCodeAsync(commonCode);
            
            if (role == null)
            {
                return new ResultModel<RoleDto>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ResponseCode = "NOT_FOUND",
                    Message = $"Không tìm thấy role với CommonCode: {commonCode}"
                };
            }

            var roleDto = _mapper.Map<RoleDto>(role);

            return new ResultModel<RoleDto>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = roleDto,
                Message = "Tìm role theo CommonCode thành công"
            };
        }

        public async Task<ResultModel<List<RoleDto>>> SearchRolesAsync(SearchRoleRequest request)
        {
            var roles = await _unitOfWork.Roles.SearchRolesAsync(request.Search, request.IsActive);
            var roleDtos = _mapper.Map<List<RoleDto>>(roles);

            return new ResultModel<List<RoleDto>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = roleDtos,
                Message = $"Tìm thấy {roleDtos.Count} role"
            };
        }

        public async Task<ResultModel<RoleDto>> CreateRoleAsync(CreateRoleRequest request, int createdBy)
        {
            // Kiểm tra RoleValue đã tồn tại chưa
            var existingRole = await _unitOfWork.Roles.GetRoleByRoleValueAsync(request.RoleValue);
            if (existingRole != null)
            {
                return new ResultModel<RoleDto>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    ResponseCode = "CONFLICT",
                    Message = $"RoleValue '{request.RoleValue}' đã tồn tại"
                };
            }

            var role = new Role
            {
                RoleValue = request.RoleValue,
                RoleName = request.RoleName,
                Description = request.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            await _unitOfWork.Roles.AddAsync(role);
            await _unitOfWork.SaveChangesAsync();

            var roleDto = _mapper.Map<RoleDto>(role);

            return new ResultModel<RoleDto>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Data = roleDto,
                Message = "Tạo role thành công"
            };
        }

        public async Task<ResultModel<RoleDto>> UpdateRoleAsync(int roleId, UpdateRoleRequest request, int updatedBy)
        {
            var role = await _unitOfWork.Roles.GetRoleByIdAsync(roleId);
            
            if (role == null)
            {
                return new ResultModel<RoleDto>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ResponseCode = "NOT_FOUND",
                    Message = "Không tìm thấy role"
                };
            }

            // Cập nhật các trường nếu có
            if (!string.IsNullOrWhiteSpace(request.RoleName))
                role.RoleName = request.RoleName;
            
            if (request.Description != null)
                role.Description = request.Description;
            
            if (request.IsActive.HasValue)
                role.IsActive = request.IsActive.Value;

            role.UpdatedAt = DateTime.UtcNow;
            role.UpdatedBy = updatedBy;

            await _unitOfWork.Roles.UpdateAsync(role);
            await _unitOfWork.SaveChangesAsync();

            var roleDto = _mapper.Map<RoleDto>(role);

            return new ResultModel<RoleDto>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = roleDto,
                Message = "Cập nhật role thành công"
            };
        }

        public async Task<ResultModel> DeleteRoleAsync(int roleId)
        {
            var role = await _unitOfWork.Roles.GetRoleByIdAsync(roleId);
            
            if (role == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ResponseCode = "NOT_FOUND",
                    Message = "Không tìm thấy role"
                };
            }

            // Kiểm tra xem role có đang được sử dụng không
            if (role.AccountRoles.Any())
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    ResponseCode = "CONFLICT",
                    Message = $"Không thể xóa role vì đang có {role.AccountRoles.Count} tài khoản sử dụng"
                };
            }

            await _unitOfWork.Roles.DeleteAsync(role);
            await _unitOfWork.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa role thành công"
            };
        }

        public async Task<ResultModel<List<RoleDto>>> GetRolesByAccountIdAsync(int accountId)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
            
            if (account == null)
            {
                return new ResultModel<List<RoleDto>>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ResponseCode = "NOT_FOUND",
                    Message = "Không tìm thấy tài khoản"
                };
            }

            var roles = await _unitOfWork.Roles.GetRolesByAccountIdAsync(accountId);
            var roleDtos = _mapper.Map<List<RoleDto>>(roles);

            return new ResultModel<List<RoleDto>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = roleDtos,
                Message = $"Tài khoản có {roleDtos.Count} role"
            };
        }

        public async Task<ResultModel> AssignRolesToAccountAsync(AssignRolesRequest request, int assignedBy)
        {
            // Kiểm tra account có tồn tại không
            var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
            
            if (account == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ResponseCode = "NOT_FOUND",
                    Message = "Không tìm thấy tài khoản"
                };
            }

            // Kiểm tra các role có tồn tại không
            var invalidRoleIds = new List<int>();
            foreach (var roleId in request.RoleIds)
            {
                var role = await _unitOfWork.Roles.GetRoleByIdAsync(roleId);
                if (role == null || !role.IsActive)
                {
                    invalidRoleIds.Add(roleId);
                }
            }

            if (invalidRoleIds.Any())
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    ResponseCode = "BAD_REQUEST",
                    Message = $"Các RoleId không hợp lệ: {string.Join(", ", invalidRoleIds)}"
                };
            }

            // Xóa tất cả role cũ
            await _unitOfWork.Roles.RemoveAllAccountRolesAsync(request.AccountId);

            // Thêm các role mới
            foreach (var roleId in request.RoleIds.Distinct())
            {
                await _unitOfWork.Roles.AddAccountRoleAsync(request.AccountId, roleId);
            }

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Đã gán {request.RoleIds.Count} role cho tài khoản thành công"
            };
        }
    }
}
