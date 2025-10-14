using System.Security.Claims;
using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.AccountServices;

public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AccountService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ResultModel> GetCustomerProfileAsync(int accountId)
        {
            var account = await _unitOfWork.Accounts.GetSingleAsync(
                a => a.AccountId == accountId,
                a => a.Customer
            );
            if (account == null || account.Customer == null)
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Khách hàng"),
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            var accountDto = _mapper.Map<AccountDto>(account);
            var customerDto = _mapper.Map<CustomerDto>(account.Customer);
            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = new { Account = accountDto, Customer = customerDto },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> EditCustomerProfileAsync(EditCustomerProfileRequest request)
        {
            var account = await _unitOfWork.Accounts.GetSingleAsync(
                a => a.AccountId == request.AccountId,
                a => a.Customer
            );
            if (account == null || account.Customer == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Khách hàng"),
                    StatusCodes.Status404NotFound
                );
            account.Email = request.Email ?? account.Email;
            account.Phone = request.Phone ?? account.Phone;
            account.Customer.FullName = request.FullName ?? account.Customer.FullName;
            account.Customer.IdentityCard = request.IdentityCard ?? account.Customer.IdentityCard;
            account.Customer.Address = request.Address ?? account.Customer.Address;

            // Avatar logic
            if (!string.IsNullOrEmpty(request.AvatarUrl))
            {
                // Remove old avatar if exists
                if (account.Customer.AvatarMediaId.HasValue)
                {
                    var oldAvatar = await _unitOfWork.Mediums.GetByIdAsync(account.Customer.AvatarMediaId.Value);
                    if (oldAvatar != null)
                    {
                        await _unitOfWork.Mediums.DeleteAsync(oldAvatar);
                    }
                }
                // Create new avatar Medium
                var newAvatar = new Medium
                {
                    CustomerId = account.Customer.CustomerId,
                    FilePath = request.AvatarUrl,
                    Description = "Avatar",
                    DisplayOrder = 0,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = account.Customer.CustomerId,
                    UpdatedAt = null,
                    UpdatedBy = null
                };
                await _unitOfWork.Mediums.AddAsync(newAvatar);
                await _unitOfWork.SaveChangesAsync();
                account.Customer.AvatarMediaId = newAvatar.MediaId;
            }

            await _unitOfWork.SaveChangesAsync();
            var accountDto = _mapper.Map<AccountDto>(account);
            var customerDto = _mapper.Map<CustomerDto>(account.Customer);
            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.UPDATE_SUCCESS,
                Data = new { Account = accountDto, Customer = customerDto },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetEmployeeProfileAsync(int accountId)
        {
            var account = await _unitOfWork.Accounts.GetSingleAsync(
                a => a.AccountId == accountId,
                a => a.Employee
            );
            if (account == null || account.Employee == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                    StatusCodes.Status404NotFound
                );
            var accountDto = _mapper.Map<AccountDto>(account);
            var employeeDto = _mapper.Map<EmployeeDto>(account.Employee);
            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = new { Account = accountDto, Employee = employeeDto },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> EditEmployeeProfileAsync(EditEmployeeProfileRequest request)
        {
            var account = await _unitOfWork.Accounts.GetSingleAsync(
                a => a.AccountId == request.AccountId,
                a => a.Employee
            );
            if (account == null || account.Employee == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                    StatusCodes.Status404NotFound
                );
            account.Email = request.Email ?? account.Email;
            account.Phone = request.Phone ?? account.Phone;
            account.Employee.FullName = request.FullName ?? account.Employee.FullName;
            if (request.EmployeeTypeId.HasValue) account.Employee.EmployeeTypeId = request.EmployeeTypeId.Value;
            if (request.HireDate.HasValue) account.Employee.HireDate = request.HireDate.Value;
            if (request.TerminationDate.HasValue) account.Employee.TerminationDate = request.TerminationDate.Value;
            await _unitOfWork.SaveChangesAsync();
            var accountDto = _mapper.Map<AccountDto>(account);
            var employeeDto = _mapper.Map<EmployeeDto>(account.Employee);
            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.UPDATE_SUCCESS,
                Data = new { Account = accountDto, Employee = employeeDto },
                StatusCode = StatusCodes.Status200OK
            };
        }
    }
