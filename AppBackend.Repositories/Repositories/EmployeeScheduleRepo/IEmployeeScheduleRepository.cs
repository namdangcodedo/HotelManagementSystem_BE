using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.EmployeeScheduleRepo
{
    public interface IEmployeeScheduleRepository : IGenericRepository<EmployeeSchedule>
    {
        /// <summary>
        /// Lấy lịch làm việc theo khoảng thời gian
        /// </summary>
        Task<IEnumerable<EmployeeSchedule>> GetSchedulesByDateRangeAsync(DateOnly startDate, DateOnly endDate);

        /// <summary>
        /// Lấy lịch làm việc của nhân viên trong khoảng thời gian
        /// </summary>
        Task<IEnumerable<EmployeeSchedule>> GetEmployeeSchedulesByDateRangeAsync(int employeeId, DateOnly startDate, DateOnly endDate);

        /// <summary>
        /// Kiểm tra xem nhân viên có bị trùng lịch làm việc không
        /// </summary>
        Task<bool> HasConflictingScheduleAsync(int employeeId, DateOnly shiftDate, TimeOnly startTime, TimeOnly endTime, int? excludeScheduleId = null);

        /// <summary>
        /// Lấy danh sách nhân viên có thể làm việc (không bị trùng lịch)
        /// </summary>
        Task<IEnumerable<Employee>> GetAvailableEmployeesAsync(DateOnly shiftDate, TimeOnly startTime, TimeOnly endTime, int? employeeTypeId = null);
    }
}