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

            // Lấy tất cả schedules của nhân viên trong ngày đó
            var existingSchedules = await query.ToListAsync();
            
            // Không có lịch nào trong ngày → không conflict
            if (!existingSchedules.Any())
            {
                return false;
            }

            // Chuyển đổi TimeOnly sang phút để so sánh dễ hơn
            // Xử lý đặc biệt cho ca đêm kết thúc lúc 00:00 (coi như 24:00 = 1440 phút)
            int newStartMinutes = startTime.Hour * 60 + startTime.Minute;
            int newEndMinutes = endTime == new TimeOnly(0, 0) ? 24 * 60 : endTime.Hour * 60 + endTime.Minute;

            foreach (var es in existingSchedules)
            {
                int existingStartMinutes = es.StartTime.Hour * 60 + es.StartTime.Minute;
                int existingEndMinutes = es.EndTime == new TimeOnly(0, 0) ? 24 * 60 : es.EndTime.Hour * 60 + es.EndTime.Minute;

                // Kiểm tra overlap: hai khoảng thời gian [A, B) và [C, D) overlap khi A < D và C < B
                bool hasOverlap = newStartMinutes < existingEndMinutes && existingStartMinutes < newEndMinutes;
                
                if (hasOverlap)
                {
                    return true;
                }
            }

            return false;
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
