using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.BusinessObjects.Enums;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.AttendanceModel;
using AppBackend.Services.ApiModels.EmployeeModel;
using AppBackend.Services.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;

namespace AppBackend.Services.Services.AttendanceServices
{
    public class AttendanceService : IAttendaceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly EncryptHelper _encryptHelper;
        private readonly PaginationHelper _paginationHelper;

        public AttendanceService(IUnitOfWork unitOfWork, IMapper mapper, EncryptHelper encryptHelper, PaginationHelper paginationHelper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _encryptHelper = encryptHelper;
            _paginationHelper = paginationHelper;
        }

        public async Task<ResultModel> GetEmployeeAttendance(GetAttendanceRequest request)
        {
            var attendances = await _unitOfWork.AttendenceRepository.GetAttendancesWithEmployee();

            if (!string.IsNullOrEmpty(request.EmployeeName))
            {
                attendances = attendances.Where(a => a.Employee.FullName.Contains(request.EmployeeName)).ToList();
            }

            if (request.EmployeeId != null)
            {
                attendances = attendances.Where(a => a.EmployeeId == request.EmployeeId).ToList();
            }

            if (request.Month != null)
            {
                attendances = attendances.Where(a => a.Workdate.Month == request.Month).ToList();
            }

            if (request.Year != null)
            {
                attendances = attendances.Where(a => a.Workdate.Year == request.Year).ToList();
            }

            if (request.workDate != null)
            {
                attendances = attendances.Where(a => a.Workdate == request.workDate).ToList();

            }

            var attendanceDtos = _mapper.Map<List<AttendanceDTO>>(attendances);
            var pageAttendance = _paginationHelper.HandlePagination(attendanceDtos.Cast<dynamic>().ToList(), request.PageIndex, request.PageSize);
            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Danh sách attendance",
                Data = pageAttendance,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetEmployeeAttendInfo(GetAttendanceRequest request)
        {
            var employeeId = request.EmployeeId;
            if (employeeId != null)
            {
                var employee = await _unitOfWork.Employees.GetSingleAsync(e => e.EmployeeId == employeeId);
                if (employee != null)
                {
                    var attendInfos = await _unitOfWork.AttendenceRepository.GetAttendInfosByEmployeeId((int)employeeId, request.Year);
                    var attendInfoDTOs = _mapper.Map<List<EmpAttendInfoDTO>>(attendInfos);
                    var pageAttendInfoDTO = _paginationHelper.HandlePagination(attendInfoDTOs.Cast<dynamic>().ToList(), request.PageIndex, request.PageSize);

                    return new ResultModel
                    {
                        IsSuccess = true,
                        ResponseCode = CommonMessageConstants.SUCCESS,
                        Message = "Attend Infos",
                        Data = pageAttendInfoDTO,
                        StatusCode = StatusCodes.Status200OK
                    };
                }
            }
            return new ResultModel
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.NOT_FOUND,
                Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                Data = null,
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        public async Task<ResultModel> HandelTxtData(String txtdata)
        {
            if (string.IsNullOrWhiteSpace(txtdata))
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.INVALID,
                    Message = "Input data is empty",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Normalize line endings and split into non-empty lines
            var lines = txtdata
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            // Temporary storage grouped by employeeId and workdate
            var grouped = new Dictionary<(int employeeId, DateTime date), List<DateTime>>();

            foreach (var line in lines)
            {
                var parts = line.Split('|', StringSplitOptions.None).Select(p => p.Trim()).ToArray();
                if (parts.Length == 0) continue;

                // First column is employee code (string)
                var codeToken = parts[0];

                // Try to find a datetime token in the line (robust to which column it's in)
                DateTime timestamp;
                string? dateToken = parts.FirstOrDefault(p => DateTime.TryParse(p, out _));
                if (dateToken == null)
                {
                    // no timestamp found -> skip
                    continue;
                }
                DateTime.TryParse(dateToken, out timestamp);

                // Try to determine EmployeeId:
                // 1) prefer numeric token (commonly present) that maps to an existing employee
                // 2) fallback: try parse first column (remove leading zeros) as id
                int? employeeId = null;

                // search tokens for integer that corresponds to an EmployeeId in DB
                foreach (var p in parts)
                {
                    if (int.TryParse(p, out var parsedId))
                    {
                        var empCheck = await _unitOfWork.Employees.GetSingleAsync(e => e.EmployeeId == parsedId);
                        if (empCheck != null)
                        {
                            employeeId = parsedId;
                            break;
                        }
                    }
                }

                // fallback: try parse first column after trimming leading zeros
                if (employeeId == null)
                {
                    var trimmed = codeToken.TrimStart('0');
                    if (int.TryParse(trimmed, out var parsedFromCode))
                    {
                        var empCheck = await _unitOfWork.Employees.GetSingleAsync(e => e.EmployeeId == parsedFromCode);
                        if (empCheck != null)
                        {
                            employeeId = parsedFromCode;
                        }
                    }
                }

                // If still not found, skip this line (unknown employee)
                if (employeeId == null) continue;

                var dateOnly = timestamp.Date;
                var key = ((int)employeeId, dateOnly);

                if (!grouped.ContainsKey(key))
                    grouped[key] = new List<DateTime>();

                grouped[key].Add(timestamp);
            }

            // Process grouped records: keep earliest time as CheckIn and latest time as CheckOut per employee/day
            foreach (var kvp in grouped)
            {
                var employeeId = kvp.Key.employeeId;
                var date = kvp.Key.date;
                var timestamps = kvp.Value;

                var earliest = timestamps.Min();
                var latest = timestamps.Max();

                // Try to find existing attendance for that employee and date
                var existing = await _unitOfWork.AttendenceRepository
                    .GetSingleAsync(a =>
                        a.EmployeeId == employeeId &&
                        a.Workdate.Year == date.Year &&
                        a.Workdate.Month == date.Month &&
                        a.Workdate.Day == date.Day);

                if (existing != null)
                {
                    // update earliest check-in / latest check-out
                    var existingCheckIn = existing.CheckIn;
                    var existingCheckOut = existing.CheckOut;

                    var newCheckIn = TimeOnly.FromTimeSpan(earliest.TimeOfDay);
                    var newCheckOut = TimeOnly.FromTimeSpan(latest.TimeOfDay);

                    // If existing has a check-in that is earlier, keep it; otherwise use newCheckIn
                    if (newCheckIn < existingCheckIn)
                        existing.CheckIn = newCheckIn;

                    // Update CheckOut to be the latest known time
                    if (existingCheckOut == null || newCheckOut > existingCheckOut)
                        existing.CheckOut = newCheckOut;

                    existing.UpdatedAt = DateTime.Now;
                    existing.Status = AttendanceStatus.Attended.ToString();
                    existing.IsApproved = ApprovalStatus.Approved.ToString();

                    await _unitOfWork.AttendenceRepository.UpdateAsync(existing);
                }
                else
                {
                    // create new attendance entry
                    var attendanceRecord = new Attendance
                    {
                        EmployeeId = employeeId,
                        DeviceEmployeeId = null,
                        CheckIn = TimeOnly.FromTimeSpan(earliest.TimeOfDay),
                        CheckOut = (latest > earliest) ? TimeOnly.FromTimeSpan(latest.TimeOfDay) : null,
                        Workdate = date,
                        Status = AttendanceStatus.Attended.ToString(),
                        IsApproved = ApprovalStatus.Approved.ToString(),
                        CreatedAt = DateTime.Now
                    };

                    await _unitOfWork.AttendenceRepository.AddAsync(attendanceRecord);
                }
            }

            // Persist changes
            await _unitOfWork.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Insert attendances thành công",
                Data = null,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> InsertAttendances(PostAttendancesRequest request)
        {
            foreach (var attendance in request.Attendances)
            {
                var result = await UpsertAttendance(attendance);
                if (result.IsSuccess == false)
                {
                    return result;
                }
            }

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Insert attendances thành công",
                Data = null,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> UpsertAttendance(PostAttendanceRequest request)
        {
            var employeeId = request.EmployeeId;
            var attendanceId = request.AttendanceId;
            var employee = await _unitOfWork.Employees.GetSingleAsync(e => e.EmployeeId == employeeId);
            if (employee != null)
            {
                if (attendanceId != null && attendanceId != 0)
                {
                    var existingAttendance = await _unitOfWork.AttendenceRepository.GetSingleAsync(a => a.AttendanceId == attendanceId);
                    if (existingAttendance != null)
                    {
                        _mapper.Map(request, existingAttendance);
                        await _unitOfWork.AttendenceRepository.UpdateAsync(existingAttendance);
                        await _unitOfWork.SaveChangesAsync();
                        return new ResultModel
                        {
                            IsSuccess = true,
                            ResponseCode = CommonMessageConstants.SUCCESS,
                            Message = "Cập nhập bản ghi chấm công thành công",
                            Data = null,
                            StatusCode = StatusCodes.Status404NotFound
                        };
                    }
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Attendance"),
                        Data = null,
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }
                else
                {
                    //init new attendance

                    //handle attendance data
                    var attendance = _mapper.Map<Attendance>(request);
                    var resultModel = await handleInitAttendance(attendance);
                    _unitOfWork.AttendenceRepository.AddAsync(attendance);
                    _unitOfWork.SaveChangesAsync();

                    return resultModel;
                }

            }

            return new ResultModel
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.NOT_FOUND,
                Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                Data = null,
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        public async Task<ResultModel> handleInitAttendance(Attendance entity)
        {
            entity.CreatedAt = DateTime.Now;
            if (entity.Status.Equals(AttendanceStatus.Attended))
            {
                entity.IsApproved = ApprovalStatus.Approved.ToString();
                if (entity.CheckIn == null || entity.CheckOut == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.INVALID,
                        Message = "CheckIn và CheckOut không được để trống khi trạng thái là Attended",
                        Data = null,
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }

                if (entity.CheckOut <= entity.CheckIn)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.INVALID,
                        Message = "CheckOut phải lớn hơn CheckIn",
                        Data = null,
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
            }
            else if (entity.Status.Equals(AttendanceStatus.AbsentWithLeave) ||
                entity.Status.Equals(AttendanceStatus.AbsentWithoutLeave))
            {
                entity.IsApproved = ApprovalStatus.Pending.ToString();
                //xử lý attendInfo
            }

            await _unitOfWork.AttendenceRepository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Upsert attendance thành công",
                Data = null,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> UpsertAttendInfo(PostAttendInfoRequest request)
        {
            var employeeId = request.EmployeeId;
            var attendInfoId = request.AttendInfoId;
            var employee = await _unitOfWork.Employees.GetSingleAsync(e => e.EmployeeId == employeeId);
            if (employee != null)
            {
                var attendInfo = _mapper.Map<EmpAttendInfo>(request);
                _unitOfWork.AttendenceRepository.UpsertAttendInfo(attendInfo);
                _unitOfWork.SaveChangesAsync();

                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Upsert AttendInfo thành công",
                    Data = null,
                    StatusCode = StatusCodes.Status201Created
                };
            }

            return new ResultModel
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.NOT_FOUND,
                Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                Data = null,
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        public async Task<ResultModel> GetStaticInfo(GetAttendanceRequest request)
        {
            var attendance = await _unitOfWork.AttendenceRepository.GetAllAsync();
            var attendanceAttend = attendance.Where(a => a.Status.Equals(AttendanceStatus.Attended.ToString())).ToList();
            var attendanceAbsentWithoutLeave = attendance.Where(a => a.Status.Equals(AttendanceStatus.AbsentWithoutLeave.ToString())).ToList();
            var attendanceAbsentWithLeave = attendance.Where(a => a.Status.Equals(AttendanceStatus.AbsentWithLeave.ToString())).ToList();

            var staticModel = new AttendanceStatic
            {
                attendance = attendance.Count(),
                attend = attendanceAttend.Count(),
                absentWithLeave = attendanceAbsentWithLeave.Count(),
                absentWithoutLeave = attendanceAbsentWithoutLeave.Count()
            };

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "",
                Data = staticModel,
                StatusCode = StatusCodes.Status201Created
            };
        }

    }

    public class AttendanceStatic
    {
        public int attendance { get; set; }
        public int attend { get; set; }
        public int absentWithLeave { get; set; }
        public int absentWithoutLeave { get; set; }

    }
}
