using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.BusinessObjects.Enums;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.EmployeeModel;
using AppBackend.Services.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq;

namespace AppBackend.Services.Services.EmployeeServices
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly AccountHelper _accountHelper;

        public EmployeeService(IUnitOfWork unitOfWork, IMapper mapper, AccountHelper accountHelper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _accountHelper = accountHelper;
        }

        public async Task<ResultModel> GetEmployeeDetailAsync(int employeeId)
        {
            var employee = await _unitOfWork.Employees.GetEmployeeWithAccountAsync(employeeId);
            
            if (employee == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            var employeeDto = _mapper.Map<EmployeeDto>(employee);
            var accountDto = _mapper.Map<AccountDto>(employee.Account);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = new { Employee = employeeDto, Account = accountDto },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetEmployeeListAsync(GetEmployeeRequest request)
        {
            var query = await _unitOfWork.Employees.FindAsync(e => true);
            var employees = query.AsQueryable();

            // Lọc theo loại nhân viên
            if (request.EmployeeTypeId.HasValue)
            {
                employees = employees.Where(e => e.EmployeeTypeId == request.EmployeeTypeId.Value);
            }

            // Lọc theo trạng thái hoạt động (chưa bị sa thải)
            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value)
                {
                    employees = employees.Where(e => e.TerminationDate == null);
                }
                else
                {
                    employees = employees.Where(e => e.TerminationDate != null);
                }
            }

            // Tìm kiếm theo tên hoặc số điện thoại
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                employees = employees.Where(e => 
                    e.FullName.Contains(request.Search) || 
                    (e.PhoneNumber != null && e.PhoneNumber.Contains(request.Search)));
            }

            // Sắp xếp
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                //employees = request.SortDesc
                //    ? employees.OrderByDescending(e => EF.Property<object>(e, request.SortBy))
                //    : employees.OrderBy(e => EF.Property<object>(e, request.SortBy));
            }
            else
            {
                employees = employees.OrderByDescending(e => e.CreatedAt);
            }

            // Tổng số bản ghi
            var totalRecords = employees.Count();

            // Phân trang
            var pagedEmployees = employees
                .Skip(request.PageIndex-1 * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var employeeDtos = _mapper.Map<List<EmployeeDto>>(pagedEmployees);

            var pagedResponse = new PagedResponseDto<EmployeeDto>
            {
                Items = employeeDtos,
                TotalCount = totalRecords,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize)
            };

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = pagedResponse,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> AddEmployeeAsync(AddEmployeeRequest request)
        {
            // Kiểm tra email đã tồn tại
            var existingAccount = await _unitOfWork.Accounts.GetByEmailAsync(request.Email);
            if (existingAccount != null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.EXISTED,
                    Message = string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Email"),
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Kiểm tra loại nhân viên có tồn tại
            var employeeType = await _unitOfWork.CommonCodes.GetByIdAsync(request.EmployeeTypeId);
            if (employeeType == null || employeeType.CodeType != "EmployeeType")
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Loại nhân viên"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Tạo tài khoản
            var hashedPassword = _accountHelper.HashPassword(request.Password);
            var account = new Account
            {
                Username = request.Username,
                PasswordHash = hashedPassword,
                Email = request.Email,
                IsLocked = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null,
                UpdatedAt = null,
                UpdatedBy = null
            };

            await _unitOfWork.Accounts.AddAsync(account);
            await _unitOfWork.SaveChangesAsync();

            // Tạo nhân viên
            var employee = new Employee
            {
                AccountId = account.AccountId,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                EmployeeTypeId = request.EmployeeTypeId,
                HireDate = request.HireDate,
                TerminationDate = null,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null,
                UpdatedAt = null,
                UpdatedBy = null
            };

            await _unitOfWork.Employees.AddAsync(employee);
            await _unitOfWork.SaveChangesAsync();

            // Tự động gán vai trò dựa trên EmployeeType.CodeName = Role.RoleValue
            // Ví dụ: EmployeeType có CodeName="Manager" sẽ tìm Role có RoleValue="Manager"
            var role = await _unitOfWork.Roles.GetRoleByRoleValueAsync(employeeType.CodeName);
            if (role != null)
            {
                var accountRole = new AccountRole
                {
                    AccountId = account.AccountId,
                    RoleId = role.RoleId
                };
                await _unitOfWork.Accounts.AddAccountRoleAsync(accountRole);
                await _unitOfWork.SaveChangesAsync();
            }

            var employeeDto = _mapper.Map<EmployeeDto>(employee);
            var accountDto = _mapper.Map<AccountDto>(account);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Thêm nhân viên thành công",
                Data = new { Employee = employeeDto, Account = accountDto },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> UpdateEmployeeAsync(UpdateEmployeeRequest request)
        {
            var employee = await _unitOfWork.Employees.GetEmployeeWithAccountAsync(request.EmployeeId);
            
            if (employee == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Cập nhật thông tin Account
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                // Kiểm tra email mới có trùng với email khác không
                var existingAccount = await _unitOfWork.Accounts.GetByEmailAsync(request.Email);
                if (existingAccount != null && existingAccount.AccountId != employee.AccountId)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.EXISTED,
                        Message = string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Email"),
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
                employee.Account.Email = request.Email;
            }

            // Cập nhật thông tin Employee
            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                employee.FullName = request.FullName;
            }

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                employee.PhoneNumber = request.PhoneNumber;
            }

            if (request.EmployeeTypeId.HasValue)
            {
                // Kiểm tra loại nhân viên có tồn tại
                var employeeType = await _unitOfWork.CommonCodes.GetByIdAsync(request.EmployeeTypeId.Value);
                if (employeeType == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Loại nhân viên"),
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }
                employee.EmployeeTypeId = request.EmployeeTypeId.Value;
            }

            if (request.HireDate.HasValue)
            {
                employee.HireDate = request.HireDate.Value;
            }

            if (request.TerminationDate.HasValue)
            {
                employee.TerminationDate = request.TerminationDate.Value;
            }

            employee.UpdatedAt = DateTime.UtcNow;
            employee.Account.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Employees.UpdateAsync(employee);
            await _unitOfWork.Accounts.UpdateAsync(employee.Account);
            await _unitOfWork.SaveChangesAsync();

            var employeeDto = _mapper.Map<EmployeeDto>(employee);
            var accountDto = _mapper.Map<AccountDto>(employee.Account);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Cập nhật nhân viên thành công",
                Data = new { Employee = employeeDto, Account = accountDto },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> BanEmployeeAsync(BanEmployeeRequest request)
        {
            var employee = await _unitOfWork.Employees.GetEmployeeWithAccountAsync(request.EmployeeId);
            
            if (employee == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            employee.Account.IsLocked = request.IsLocked;
            employee.Account.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Accounts.UpdateAsync(employee.Account);
            await _unitOfWork.SaveChangesAsync();

            var message = request.IsLocked ? "Khoá tài khoản nhân viên thành công" : "Mở khoá tài khoản nhân viên thành công";

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = message,
                Data = new { employee.EmployeeId, IsLocked = employee.Account.IsLocked },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> SearchEmployeesAsync(SearchEmployeeRequest request)
        {
            var employees = await _unitOfWork.Employees.SearchEmployeesAsync(
                request.Keyword,
                request.EmployeeTypeId,
                request.IsActive,
                request.IsLocked
            );

            // Map sang DTO với thông tin đầy đủ
            var searchResults = employees.Select(e => new EmployeeSearchResultDto
            {
                EmployeeId = e.EmployeeId,
                AccountId = e.AccountId,
                FullName = e.FullName,
                PhoneNumber = e.PhoneNumber,
                EmployeeTypeId = e.EmployeeTypeId,
                EmployeeTypeName = e.EmployeeType.CodeValue,
                HireDate = e.HireDate,
                TerminationDate = e.TerminationDate,
                BaseSalary = e.BaseSalary,
                Username = e.Account.Username,
                Email = e.Account.Email,
                IsLocked = e.Account.IsLocked,
                LastLoginAt = e.Account.LastLoginAt,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            }).ToList();

            // Phân trang
            var totalRecords = searchResults.Count;
            var pagedResults = searchResults
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var pagedResponse = new PagedResponseDto<EmployeeSearchResultDto>
            {
                Items = pagedResults,
                TotalCount = totalRecords,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize)
            };

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Tìm thấy {totalRecords} nhân viên",
                Data = pagedResponse,
                StatusCode = StatusCodes.Status200OK
            };
        }
    }
}
