using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.EmployeeModel;

namespace AppBackend.Services.Services.EmployeeServices
{
    public interface IEmployeeService
    {
        Task<ResultModel> GetEmployeeDetailAsync(int employeeId);
        Task<ResultModel> GetEmployeeListAsync(GetEmployeeRequest request);
        Task<ResultModel> AddEmployeeAsync(AddEmployeeRequest request);
        Task<ResultModel> UpdateEmployeeAsync(UpdateEmployeeRequest request);
        Task<ResultModel> BanEmployeeAsync(BanEmployeeRequest request);

        /// <summary>
        /// Tìm kiếm nhân viên theo keyword trên tất cả các trường thông tin
        /// </summary>
        Task<ResultModel> SearchEmployeesAsync(SearchEmployeeRequest request);
    }
}
