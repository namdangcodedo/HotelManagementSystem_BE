using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.EmployeeRepo
{
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        Task<Employee?> GetEmployeeByAccountIdAsync(int accountId);
        Task<Employee?> GetEmployeeWithAccountAsync(int employeeId);
    }
}

