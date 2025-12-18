using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.EmployeeScheduleRepo
{
    public class EmployeeScheduleRepository : GenericRepository<EmployeeSchedule>, IEmployeeScheduleRepository
    {
        public EmployeeScheduleRepository(HotelManagementContext context) : base(context)
        {
        }

        public async Task<IEnumerable<EmployeeSchedule>> GetSchedulesByDateRangeAsync(DateOnly startDate, DateOnly endDate)
        {
            return await Context.EmployeeSchedules
                .Include(es => es.Employee)
                    .ThenInclude(e => e.EmployeeType)
                .Where(es => es.ShiftDate >= startDate && es.ShiftDate <= endDate)
                .OrderBy(es => es.ShiftDate)
                    .ThenBy(es => es.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<EmployeeSchedule>> GetEmployeeSchedulesByDateRangeAsync(int employeeId, DateOnly startDate, DateOnly endDate)
        {
            return await Context.EmployeeSchedules
                .Include(es => es.Employee)
                    .ThenInclude(e => e.EmployeeType)
                .Where(es => es.EmployeeId == employeeId && es.ShiftDate >= startDate && es.ShiftDate <= endDate)
                .OrderBy(es => es.ShiftDate)
                    .ThenBy(es => es.StartTime)
                .ToListAsync();
        }

        public async Task<bool> HasConflictingScheduleAsync(int employeeId, DateOnly shiftDate, TimeOnly startTime, TimeOnly endTime, int? excludeScheduleId = null)
        {
            var query = Context.EmployeeSchedules
                .Where(es => es.EmployeeId == employeeId && es.ShiftDate == shiftDate);

            if (excludeScheduleId.HasValue)
            {
                query = query.Where(es => es.ScheduleId != excludeScheduleId.Value);
            }

            // Kiểm tra xem có lịch nào bị trùng không
            // Trùng lịch khi:
            // 1. StartTime của lịch mới nằm trong khoảng [StartTime, EndTime] của lịch đã có
            // 2. EndTime của lịch mới nằm trong khoảng [StartTime, EndTime] của lịch đã có
            // 3. Lịch mới bao phủ hoàn toàn lịch đã có
            var hasConflict = await query.AnyAsync(es =>
                (startTime >= es.StartTime && startTime < es.EndTime) ||
                (endTime > es.StartTime && endTime <= es.EndTime) ||
                (startTime <= es.StartTime && endTime >= es.EndTime)
            );

            return hasConflict;
        }

        public async Task<IEnumerable<Employee>> GetAvailableEmployeesAsync(DateOnly shiftDate, TimeOnly startTime, TimeOnly endTime, int? employeeTypeId = null)
        {
            // Lấy danh sách nhân viên đang hoạt động (chưa bị sa thải)
            var employeesQuery = Context.Employees
                .Include(e => e.EmployeeType)
                .Include(e => e.Account)
                .Where(e => e.TerminationDate == null && !e.Account.IsLocked);

            // Lọc theo loại nhân viên nếu có
            if (employeeTypeId.HasValue)
            {
                employeesQuery = employeesQuery.Where(e => e.EmployeeTypeId == employeeTypeId.Value);
            }

            var activeEmployees = await employeesQuery.ToListAsync();

            // Lấy danh sách ID nhân viên đã có lịch trùng
            var busyEmployeeIds = await Context.EmployeeSchedules
                .Where(es => es.ShiftDate == shiftDate &&
                    ((startTime >= es.StartTime && startTime < es.EndTime) ||
                     (endTime > es.StartTime && endTime <= es.EndTime) ||
                     (startTime <= es.StartTime && endTime >= es.EndTime)))
                .Select(es => es.EmployeeId)
                .ToListAsync();

            // Lọc bỏ những nhân viên đã bận
            var availableEmployees = activeEmployees
                .Where(e => !busyEmployeeIds.Contains(e.EmployeeId))
                .ToList();

            return availableEmployees;
        }
    }
}
