using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.AccountModel;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.AccountServices;

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
        account.Customer.PhoneNumber = request.PhoneNumber ?? account.Customer.PhoneNumber;
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
                ReferenceKey = account.Customer.CustomerId.ToString(),
                ReferenceTable = nameof(account.Customer),
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
        account.Employee.PhoneNumber = request.Phone ?? account.Employee.PhoneNumber;
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

    public async Task<ResultModel> GetAccountSummaryAsync(int accountId, int? requesterId = null)
    {
        // Lấy account với includes
        var account = await _unitOfWork.Accounts.GetSingleAsync(
            a => a.AccountId == accountId,
            a => a.Customer,
            a => a.Employee
        );

        if (account == null)
        {
            return new ResultModel
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.NOT_FOUND,
                Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Tài khoản"),
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        // Lấy roles
        var roleNames = await _unitOfWork.Accounts.GetRoleNamesByAccountIdAsync(accountId);
        
        // Xác định account type và lấy profile details
        string accountType = "";
        object? profileDetails = null;
        AccountStatistics? statistics = null;

        if (account.Customer != null)
        {
            accountType = "Customer";
            
            // Lấy thông tin Customer
            var customerDetail = new CustomerDetailResponse
            {
                CustomerId = account.Customer.CustomerId,
                FullName = account.Customer.FullName,
                PhoneNumber = account.Customer.PhoneNumber,
                IdentityCard = account.Customer.IdentityCard,
                Address = account.Customer.Address,
                AvatarUrl = account.Customer.AvatarMediaId.HasValue 
                    ? (await _unitOfWork.Mediums.GetByIdAsync(account.Customer.AvatarMediaId.Value))?.FilePath 
                    : null
            };
            profileDetails = customerDetail;

            // Nếu requester là Admin, lấy statistics
            if (requesterId.HasValue)
            {
                var requesterRoles = await _unitOfWork.Accounts.GetRoleNamesByAccountIdAsync(requesterId.Value);
                if (requesterRoles.Contains("Admin"))
                {
                    statistics = await GetCustomerStatisticsAsync(account.Customer.CustomerId);
                }
            }
        }
        else if (account.Employee != null)
        {
            accountType = "Employee";
            
            // Lấy thông tin Employee
            var employeeType = await _unitOfWork.CommonCodes.GetByIdAsync(account.Employee.EmployeeTypeId);
            var employeeDetail = new EmployeeDetailResponse
            {
                EmployeeId = account.Employee.EmployeeId,
                FullName = account.Employee.FullName,
                PhoneNumber = account.Employee.PhoneNumber,
                EmployeeTypeId = account.Employee.EmployeeTypeId,
                EmployeeTypeName = employeeType?.CodeValue,
                HireDate = account.Employee.HireDate,
                TerminationDate = account.Employee.TerminationDate
            };
            profileDetails = employeeDetail;

            // Nếu requester là Admin, lấy statistics
            if (requesterId.HasValue)
            {
                var requesterRoles = await _unitOfWork.Accounts.GetRoleNamesByAccountIdAsync(requesterId.Value);
                if (requesterRoles.Contains("Admin"))
                {
                    statistics = await GetEmployeeStatisticsAsync(account.Employee.EmployeeId);
                }
            }
        }

        var summary = new AccountSummaryResponse
        {
            AccountId = account.AccountId,
            Username = account.Username,
            Email = account.Email,
            IsLocked = account.IsLocked,
            LastLoginAt = account.LastLoginAt,
            CreatedAt = account.CreatedAt,
            Roles = roleNames,
            AccountType = accountType,
            ProfileDetails = profileDetails,
            Statistics = statistics
        };

        return new ResultModel
        {
            IsSuccess = true,
            ResponseCode = CommonMessageConstants.SUCCESS,
            Message = CommonMessageConstants.GET_SUCCESS,
            Data = summary,
            StatusCode = StatusCodes.Status200OK
        };
    }

    private async Task<AccountStatistics> GetCustomerStatisticsAsync(int customerId)
    {
        var customer = await _unitOfWork.Customers.GetSingleAsync(
            c => c.CustomerId == customerId,
            c => c.Bookings,
            c => c.Feedbacks,
            c => c.Account
        );

        if (customer == null)
        {
            return new AccountStatistics();
        }

        var totalBookings = customer.Bookings?.Count ?? 0;
        var completedBookings = customer.Bookings?.Count(b => b.Status != null) ?? 0;
        var cancelledBookings = 0; // Cần implement logic check cancelled status

        // Tính tổng chi tiêu
        decimal totalSpent = 0;
        if (customer.Bookings != null)
        {
            foreach (var booking in customer.Bookings)
            {
                totalSpent += booking.TotalAmount;
            }
        }

        var totalFeedbacks = customer.Feedbacks?.Count ?? 0;

        // Đếm notifications
        var notifications = await _unitOfWork.Accounts.GetSingleAsync(
            a => a.AccountId == customer.AccountId,
            a => a.Notifications
        );
        var totalNotifications = notifications?.Notifications?.Count ?? 0;
        var unreadNotifications = notifications?.Notifications?.Count(n => !n.IsRead) ?? 0;

        return new AccountStatistics
        {
            TotalBookings = totalBookings,
            CompletedBookings = completedBookings,
            CancelledBookings = cancelledBookings,
            TotalSpent = totalSpent,
            TotalFeedbacks = totalFeedbacks,
            TotalNotifications = totalNotifications,
            UnreadNotifications = unreadNotifications
        };
    }

    private async Task<AccountStatistics> GetEmployeeStatisticsAsync(int employeeId)
    {
        var employee = await _unitOfWork.Employees.GetSingleAsync(
            e => e.EmployeeId == employeeId,
            e => e.HousekeepingTasks,
            e => e.Attendances,
            e => e.SalaryRecords,
            e => e.Account
        );

        if (employee == null)
        {
            return new AccountStatistics();
        }

        var totalTasks = employee.HousekeepingTasks?.Count ?? 0;
        var completedTasks = employee.HousekeepingTasks?.Count(t => t.CompletedAt != null) ?? 0;
        var pendingTasks = totalTasks - completedTasks;

        var totalAttendance = employee.Attendances?.Count ?? 0;
        var totalSalaryPaid = employee.SalaryRecords?.Sum(s => (decimal?)s.PaidAmount) ?? 0;

        // Tính số ngày làm việc
        var workingDays = (DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - employee.HireDate.DayNumber);

        // Đếm notifications
        var notifications = await _unitOfWork.Accounts.GetSingleAsync(
            a => a.AccountId == employee.AccountId,
            a => a.Notifications
        );
        var totalNotifications = notifications?.Notifications?.Count ?? 0;
        var unreadNotifications = notifications?.Notifications?.Count(n => !n.IsRead) ?? 0;

        return new AccountStatistics
        {
            TotalTasksAssigned = totalTasks,
            CompletedTasks = completedTasks,
            PendingTasks = pendingTasks,
            TotalAttendance = totalAttendance,
            TotalSalaryPaid = totalSalaryPaid,
            WorkingDays = workingDays,
            TotalNotifications = totalNotifications,
            UnreadNotifications = unreadNotifications
        };
    }
}
