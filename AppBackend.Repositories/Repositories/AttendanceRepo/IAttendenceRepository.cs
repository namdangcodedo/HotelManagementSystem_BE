using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.AttendanceRepo
{
    public interface IAttendenceRepository : IGenericRepository<Attendance>
    {
        Task<List<Attendance>> GetAttendancesByEmployeeId(int employeeId, int? month = null, int? year = null);
        Task<List<Attendance>> GetAttendancesWithEmployee();
        Task<List<EmpAttendInfo>> GetAttendInfosByEmployeeId(int employeeId, int? year = null);
        Task<List<Attendance>> GetAttendancesByEmployeeIdAndMonth(int employeeId, int? month = null);
        Task<List<Attendance>> GetAttendancesByEmployeeIdAndYear(int employeeId, int? year = null);
        Task<List<EmpAttendInfo>> GetAttendInfoByEmployeeIdAndYear(int employeeId, int? year = null);
        Task<List<EmpAttendInfo>> GetAttendInfByoEmployeeId(int employeeId);
        Task UpsertAttendances(List<Attendance>? attendances);
        Task UpsertAttendance(Attendance? attendance);
        Task UpsertAttendInfo(EmpAttendInfo? attendinfo);

    }
}
