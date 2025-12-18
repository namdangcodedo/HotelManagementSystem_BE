using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.EmployeeRepo
{
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        Task<Employee?> GetEmployeeByAccountIdAsync(int accountId);
        Task<Employee?> GetEmployeeWithAccountAsync(int employeeId);

        /// <summary>
        /// Tìm kiếm nhân viên với thông tin đầy đủ (bao gồm Account và EmployeeType)
        /// </summary>
        Task<IEnumerable<Employee>> SearchEmployeesAsync(string? keyword, int? employeeTypeId, bool? isActive, bool? isLocked);
    }
}

