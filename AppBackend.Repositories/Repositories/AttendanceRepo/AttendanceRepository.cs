using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.AttendanceRepo
{
    public class AttendanceRepository : GenericRepository<Attendance>, IAttendenceRepository
    {
        private readonly HotelManagementContext _context;
        public AttendanceRepository(HotelManagementContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Attendance>> GetAttendancesByEmployeeId(int employeeId, int? month = null, int? year = null)
        {
            var attendance = await _context.Attendances.Include(a => a.Employee).Where(a => a.EmployeeId == employeeId).ToListAsync();
            if(year != null)
            {
                attendance = attendance.Where(a => a.Workdate.Year == year).ToList();
            }

            if(month != null)
            {
                attendance = attendance.Where(a => a.Workdate.Month == month).ToList();
            }

            return attendance;
        }

        public async Task<List<Attendance>> GetAttendancesWithEmployee()
        {
            var attendance = await _context.Attendances.Include(a => a.Employee).ToListAsync();

            return attendance;
        }


        public async Task<List<EmpAttendInfo>> GetAttendInfosByEmployeeId(int employeeId, int? year = null)
        {
            var AttendInfos = await _context.EmpAttendInfo.ToListAsync();

            if (year != null)
            {
                AttendInfos = AttendInfos.Where(a => a.Year == year).ToList();
            }

            return AttendInfos;
        }

        public async Task<List<Attendance>> GetAttendancesByEmployeeIdAndMonth(int employeeId, int? month = null)
        {
            month = month ?? DateTime.Now.Month;
            return await _context.Attendances
                .Where(a => a.EmployeeId == employeeId && a.Workdate.Month == month)
                .ToListAsync();
        }

        public async Task<List<Attendance>> GetAttendancesByEmployeeIdAndYear(int employeeId, int? year = null)
        {
            year = year ?? DateTime.Now.Year;
            return await _context.Attendances
                .Where(a => a.EmployeeId == employeeId && a.Workdate.Year == year)
                .ToListAsync();
        }

        public async Task<List<EmpAttendInfo>> GetAttendInfoByEmployeeIdAndYear(int employeeId, int? year = null)
        {
            year ??= DateTime.Now.Year;
            return await _context.EmpAttendInfo
                .Where(a => a.EmployeeId == employeeId && a.Year == year)
                .ToListAsync();
        }

        public async Task<List<EmpAttendInfo>> GetAttendInfByoEmployeeId(int employeeId)
        {
            return await _context.EmpAttendInfo
                .Where(a => a.EmployeeId == employeeId)
                .ToListAsync();
        }

        public async Task UpsertAttendance(Attendance? attendance)
        {
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.AttendanceId == attendance.AttendanceId);
            if (existingAttendance != null)
            {
                _context.Entry(existingAttendance).CurrentValues.SetValues(attendance);
            }
            else
            {
                _context.Attendances.AddAsync(attendance!);
            }
            await _context.SaveChangesAsync();
        }

        public async Task UpsertAttendances(List<Attendance>? attendances)
        {
            for(int i = 0; i < attendances.Count; i++)
            {
                await UpsertAttendance (attendances[i]);
            }
        }

        public async Task UpsertAttendInfo(EmpAttendInfo? attendinfo)
        {
            var existAttendInfo = await _context.EmpAttendInfo.FirstOrDefaultAsync(a => a.AttendInfoId == attendinfo.AttendInfoId);
            if (existAttendInfo != null)
            {
                _context.Entry(existAttendInfo).CurrentValues.SetValues(attendinfo);
            }
            else
            {
                _context.EmpAttendInfo.AddAsync(attendinfo!);
            }
            await _context.SaveChangesAsync();
        }

        
    }
}
