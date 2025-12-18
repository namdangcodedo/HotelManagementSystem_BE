using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.ScheduleModel;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.ScheduleServices
{
    public class ScheduleService : IScheduleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ScheduleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResultModel<WeeklyScheduleResponse>> GetWeeklyScheduleAsync(GetWeeklyScheduleRequest request)
        {
            try
            {
                // Parse FromDate và ToDate string (yyyyMMdd)
                if (!request.TryParseDates())
                {
                    return new ResultModel<WeeklyScheduleResponse>
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.INVALID,
                        Message = "Định dạng ngày không hợp lệ. Vui lòng sử dụng format yyyyMMdd (VD: 20251216)",
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }

                // Validate request
                if (!request.IsValid() || !request.StartDate.HasValue || !request.EndDate.HasValue)
                {
                    return new ResultModel<WeeklyScheduleResponse>
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.INVALID,
                        Message = "Dữ liệu không hợp lệ. StartDate phải <= EndDate và khoảng thời gian không quá 31 ngày",
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }

                // DEBUG: Log the calculated dates
                Console.WriteLine($"[DEBUG] FromDate Input: {request.FromDate}");
                Console.WriteLine($"[DEBUG] ToDate Input: {request.ToDate}");
                Console.WriteLine($"[DEBUG] StartDate: {request.StartDate.Value}");
                Console.WriteLine($"[DEBUG] EndDate: {request.EndDate.Value}");

                // Lấy tất cả lịch làm việc trong khoảng thời gian và convert sang List ngay
                var schedules = (await _unitOfWork.EmployeeSchedules.GetSchedulesByDateRangeAsync(request.StartDate.Value, request.EndDate.Value)).ToList();
                
                Console.WriteLine($"[DEBUG] Schedules found: {schedules.Count}");

                // Filter theo EmployeeTypeId nếu có
                if (request.EmployeeTypeId.HasValue)
                {
                    schedules = schedules.Where(s => s.Employee.EmployeeTypeId == request.EmployeeTypeId.Value).ToList();
                    Console.WriteLine($"[DEBUG] After filtering by EmployeeTypeId {request.EmployeeTypeId.Value}: {schedules.Count} schedules");
                }

                // Nếu không có schedules, trả về response rỗng
                if (!schedules.Any())
                {
                    return new ResultModel<WeeklyScheduleResponse>
                    {
                        IsSuccess = true,
                        ResponseCode = CommonMessageConstants.SUCCESS,
                        Message = "Không có lịch làm việc trong khoảng thời gian này",
                        Data = new WeeklyScheduleResponse { Shifts = new List<ShiftScheduleDto>() },
                        StatusCode = StatusCodes.Status200OK
                    };
                }

                // Lấy TẤT CẢ schedules và group theo thời gian để tạo dynamic shifts
                var uniqueShifts = schedules
                    .Select(s => new { s.StartTime, s.EndTime })
                    .Distinct()
                    .OrderBy(s => s.StartTime)
                    .ToList();

                Console.WriteLine($"[DEBUG] Unique shifts found: {uniqueShifts.Count}");

                var shiftSchedules = new List<ShiftScheduleDto>();

                foreach (var shift in uniqueShifts)
                {
                    string shiftName = DetermineShiftName(shift.StartTime, shift.EndTime);
                    Console.WriteLine($"[DEBUG] Processing shift: {shiftName}");
                    
                    var dailySchedules = new List<DailyScheduleDto>();

                    // Tạo lịch cho mỗi ngày trong khoảng thời gian
                    for (var date = request.StartDate.Value; date <= request.EndDate.Value; date = date.AddDays(1))
                    {
                        var dayOfWeek = GetVietnameseDayOfWeek(date);

                        // Lọc nhân viên làm việc trong ca này vào ngày này
                        var employeesInShift = schedules
                            .Where(s => s.ShiftDate == date &&
                                   s.StartTime == shift.StartTime &&
                                   s.EndTime == shift.EndTime)
                            .Select(s => new EmployeeScheduleDto
                            {
                                ScheduleId = s.ScheduleId,
                                EmployeeId = s.EmployeeId,
                                EmployeeName = s.Employee.FullName,
                                EmployeeType = s.Employee.EmployeeType.CodeValue,
                                Status = DetermineScheduleStatus(s),
                                Notes = s.Notes
                            })
                            .ToList();
                        
                        Console.WriteLine($"[DEBUG] Date: {date}, Shift: {shiftName}, Employees: {employeesInShift.Count}");

                        dailySchedules.Add(new DailyScheduleDto
                        {
                            ShiftDate = date,
                            DayOfWeek = dayOfWeek,
                            Employees = employeesInShift
                        });
                    }

                    shiftSchedules.Add(new ShiftScheduleDto
                    {
                        ShiftName = shiftName,
                        StartTime = shift.StartTime,
                        EndTime = shift.EndTime,
                        DailySchedules = dailySchedules
                    });
                }

                var response = new WeeklyScheduleResponse
                {
                    Shifts = shiftSchedules
                };

                return new ResultModel<WeeklyScheduleResponse>
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = CommonMessageConstants.GET_SUCCESS,
                    Data = response,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ResultModel<WeeklyScheduleResponse>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = $"Lỗi khi lấy lịch làm việc: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        public async Task<ResultModel> AddScheduleAsync(AddScheduleRequest request, int createdBy)
        {
            try
            {
                // Kiểm tra nhân viên có tồn tại và đang hoạt động
                var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId);
                if (employee == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }

                if (employee.TerminationDate != null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.INVALID,
                        Message = "Không thể thêm lịch cho nhân viên đã nghỉ việc",
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }

                // Kiểm tra thời gian hợp lệ
                if (request.StartTime >= request.EndTime && request.EndTime != new TimeOnly(0, 0))
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.INVALID,
                        Message = "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc",
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }

                // Kiểm tra xung đột lịch làm việc
                var hasConflict = await _unitOfWork.EmployeeSchedules.HasConflictingScheduleAsync(
                    request.EmployeeId,
                    request.ShiftDate,
                    request.StartTime,
                    request.EndTime
                );

                if (hasConflict)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.EXISTED,
                        Message = "Nhân viên đã có lịch làm việc trùng thời gian này",
                        StatusCode = StatusCodes.Status409Conflict
                    };
                }

                // Tạo lịch làm việc mới
                var schedule = new EmployeeSchedule
                {
                    EmployeeId = request.EmployeeId,
                    ShiftDate = request.ShiftDate,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                await _unitOfWork.EmployeeSchedules.AddAsync(schedule);
                await _unitOfWork.SaveChangesAsync();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = CommonMessageConstants.CREATE_SUCCESS,
                    Data = new { ScheduleId = schedule.ScheduleId },
                    StatusCode = StatusCodes.Status201Created
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = $"Lỗi khi thêm lịch làm việc: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        public async Task<ResultModel> UpdateScheduleAsync(UpdateScheduleRequest request, int updatedBy)
        {
            try
            {
                // Parse và validate input strings
                if (!request.TryParseValues(out string? parseError))
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.INVALID,
                        Message = parseError ?? "Dữ liệu không hợp lệ",
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }

                var schedule = await _unitOfWork.EmployeeSchedules.GetByIdAsync(request.ScheduleId);
                if (schedule == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Lịch làm việc"),
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }

                // Track the changes for debugging
                Console.WriteLine($"[DEBUG] Before Update - ScheduleId: {schedule.ScheduleId}, EmployeeId: {schedule.EmployeeId}, ShiftDate: {schedule.ShiftDate}, StartTime: {schedule.StartTime}, EndTime: {schedule.EndTime}");

                // Cập nhật nhân viên nếu có
                if (request.EmployeeId.HasValue)
                {
                    var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId.Value);
                    if (employee == null)
                    {
                        return new ResultModel
                        {
                            IsSuccess = false,
                            ResponseCode = CommonMessageConstants.NOT_FOUND,
                            Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                            StatusCode = StatusCodes.Status404NotFound
                        };
                    }

                    if (employee.TerminationDate != null)
                    {
                        return new ResultModel
                        {
                            IsSuccess = false,
                            ResponseCode = CommonMessageConstants.INVALID,
                            Message = "Không thể cập nhật lịch cho nhân viên đã nghỉ việc",
                            StatusCode = StatusCodes.Status400BadRequest
                        };
                    }

                    schedule.EmployeeId = request.EmployeeId.Value;
                    Console.WriteLine($"[DEBUG] Updated EmployeeId to: {schedule.EmployeeId}");
                }

                // Cập nhật ngày làm việc (sử dụng parsed value)
                if (request.ParsedShiftDate.HasValue)
                {
                    schedule.ShiftDate = request.ParsedShiftDate.Value;
                    Console.WriteLine($"[DEBUG] Updated ShiftDate to: {schedule.ShiftDate}");
                }

                // Cập nhật thời gian (sử dụng parsed values)
                if (request.ParsedStartTime.HasValue)
                {
                    schedule.StartTime = request.ParsedStartTime.Value;
                    Console.WriteLine($"[DEBUG] Updated StartTime to: {schedule.StartTime}");
                }

                if (request.ParsedEndTime.HasValue)
                {
                    schedule.EndTime = request.ParsedEndTime.Value;
                    Console.WriteLine($"[DEBUG] Updated EndTime to: {schedule.EndTime}");
                }

                // Kiểm tra thời gian hợp lệ (cho phép ca đêm 16:00 - 00:00)
                if (schedule.StartTime >= schedule.EndTime && schedule.EndTime != new TimeOnly(0, 0))
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.INVALID,
                        Message = "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc (trừ ca đêm kết thúc 00:00)",
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }

                // Kiểm tra xung đột lịch làm việc (trừ chính nó)
                var hasConflict = await _unitOfWork.EmployeeSchedules.HasConflictingScheduleAsync(
                    schedule.EmployeeId,
                    schedule.ShiftDate,
                    schedule.StartTime,
                    schedule.EndTime,
                    schedule.ScheduleId
                );

                if (hasConflict)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.EXISTED,
                        Message = "Nhân viên đã có lịch làm việc trùng thời gian này",
                        StatusCode = StatusCodes.Status409Conflict
                    };
                }

                // Cập nhật ghi chú
                if (request.Notes != null)
                {
                    schedule.Notes = request.Notes;
                }

                schedule.UpdatedAt = DateTime.UtcNow;
                schedule.UpdatedBy = updatedBy;

                Console.WriteLine($"[DEBUG] After Update - ScheduleId: {schedule.ScheduleId}, EmployeeId: {schedule.EmployeeId}, ShiftDate: {schedule.ShiftDate}, StartTime: {schedule.StartTime}, EndTime: {schedule.EndTime}");

                // Đảm bảo entity được track và update
                await _unitOfWork.EmployeeSchedules.UpdateAsync(schedule);
                
                // Save changes và check result
                var saveResult = await _unitOfWork.SaveChangesAsync();
                Console.WriteLine($"[DEBUG] SaveChanges result: {saveResult}");

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = CommonMessageConstants.UPDATE_SUCCESS,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Update failed: {ex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = $"Lỗi khi cập nhật lịch làm việc: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        public async Task<ResultModel> DeleteScheduleAsync(int scheduleId)
        {
            try
            {
                var schedule = await _unitOfWork.EmployeeSchedules.GetByIdAsync(scheduleId);
                if (schedule == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Lịch làm việc"),
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }

                await _unitOfWork.EmployeeSchedules.DeleteAsync(schedule);
                await _unitOfWork.SaveChangesAsync();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = CommonMessageConstants.DELETE_SUCCESS,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = $"Lỗi khi xoá lịch làm việc: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        public async Task<ResultModel<AvailableEmployeesResponse>> GetAvailableEmployeesAsync(CheckAvailableEmployeesRequest request)
        {
            try
            {
                // Kiểm tra thời gian hợp lệ
                if (request.StartTime >= request.EndTime && request.EndTime != new TimeOnly(0, 0))
                {
                    return new ResultModel<AvailableEmployeesResponse>
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.INVALID,
                        Message = "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc",
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }

                var availableEmployees = await _unitOfWork.EmployeeSchedules.GetAvailableEmployeesAsync(
                    request.ShiftDate,
                    request.StartTime,
                    request.EndTime,
                    request.EmployeeTypeId
                );

                var response = new AvailableEmployeesResponse
                {
                    Employees = availableEmployees.Select(e => new AvailableEmployeeDto
                    {
                        EmployeeId = e.EmployeeId,
                        FullName = e.FullName,
                        EmployeeType = e.EmployeeType.CodeValue,
                        EmployeeTypeId = e.EmployeeTypeId,
                        PhoneNumber = e.PhoneNumber
                    }).ToList()
                };

                return new ResultModel<AvailableEmployeesResponse>
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = CommonMessageConstants.GET_SUCCESS,
                    Data = response,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ResultModel<AvailableEmployeesResponse>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = $"Lỗi khi lấy danh sách nhân viên có thể thêm: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        #region Private Helper Methods

        private string GetVietnameseDayOfWeek(DateOnly date)
        {
            return date.DayOfWeek switch
            {
                DayOfWeek.Monday => "Thứ 2",
                DayOfWeek.Tuesday => "Thứ 3",
                DayOfWeek.Wednesday => "Thứ 4",
                DayOfWeek.Thursday => "Thứ 5",
                DayOfWeek.Friday => "Thứ 6",
                DayOfWeek.Saturday => "Thứ 7",
                DayOfWeek.Sunday => "Chủ nhật",
                _ => ""
            };
        }

        private string DetermineScheduleStatus(EmployeeSchedule schedule)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (schedule.ShiftDate < today)
            {
                // Kiểm tra xem nhân viên có điểm danh không (nếu có bảng Attendance)
                // Tạm thời trả về "Hoàn thành" cho lịch đã qua
                return "Hoàn thành";
            }
            else if (schedule.ShiftDate == today)
            {
                return "Đang diễn ra";
            }
            else
            {
                return "Đã lên lịch";
            }
        }

        private string DetermineShiftName(TimeOnly startTime, TimeOnly endTime)
        {
            // Đặt tên ca dựa trên thời gian bắt đầu và kèm theo giờ cụ thể
            int hour = startTime.Hour;
            string baseName;
            
            if (hour >= 6 && hour < 14)
                baseName = "Ca Sáng";
            else if (hour >= 14 && hour < 22)
                baseName = "Ca Chiều";
            else
                baseName = "Ca Đêm";
            
            // Thêm thời gian vào tên ca để phân biệt các ca có cùng loại
            return $"{baseName} ({startTime:HH:mm} - {endTime:HH:mm})";
        }

        #endregion
    }
}
