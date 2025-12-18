using AppBackend.BusinessObjects.Dtos;
using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.ScheduleModel
{
    /// <summary>
    /// Request model để lấy lịch làm việc theo khoảng thời gian
    /// </summary>
    public class GetWeeklyScheduleRequest
    {
        /// <summary>
        /// Ngày bắt đầu - Format: yyyyMMdd (VD: 20251216 cho 16/12/2025)
        /// </summary>
        [Required(ErrorMessage = "FromDate là bắt buộc")]
        public string FromDate { get; set; } = null!;

        /// <summary>
        /// Ngày kết thúc - Format: yyyyMMdd (VD: 20251222 cho 22/12/2025)
        /// </summary>
        [Required(ErrorMessage = "ToDate là bắt buộc")]
        public string ToDate { get; set; } = null!;

        /// <summary>
        /// Lọc theo loại nhân viên (optional)
        /// </summary>
        public int? EmployeeTypeId { get; set; }

        /// <summary>
        /// StartDate được parse từ FromDate
        /// </summary>
        public DateOnly? StartDate { get; private set; }

        /// <summary>
        /// EndDate được parse từ ToDate
        /// </summary>
        public DateOnly? EndDate { get; private set; }

        /// <summary>
        /// Parse FromDate và ToDate string (yyyyMMdd) thành DateOnly
        /// </summary>
        public bool TryParseDates()
        {
            try
            {
                // Parse FromDate
                if (string.IsNullOrWhiteSpace(FromDate) || FromDate.Length != 8)
                    return false;

                int yearFrom = int.Parse(FromDate.Substring(0, 4));
                int monthFrom = int.Parse(FromDate.Substring(4, 2));
                int dayFrom = int.Parse(FromDate.Substring(6, 2));
                StartDate = new DateOnly(yearFrom, monthFrom, dayFrom);

                // Parse ToDate
                if (string.IsNullOrWhiteSpace(ToDate) || ToDate.Length != 8)
                    return false;

                int yearTo = int.Parse(ToDate.Substring(0, 4));
                int monthTo = int.Parse(ToDate.Substring(4, 2));
                int dayTo = int.Parse(ToDate.Substring(6, 2));
                EndDate = new DateOnly(yearTo, monthTo, dayTo);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate request
        /// </summary>
        public bool IsValid()
        {
            if (!StartDate.HasValue || !EndDate.HasValue)
                return false;

            // StartDate phải <= EndDate
            if (StartDate.Value > EndDate.Value)
                return false;

            // Khoảng thời gian không được quá 31 ngày
            var daysDiff = EndDate.Value.DayNumber - StartDate.Value.DayNumber;
            if (daysDiff > 31)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Response model cho lịch làm việc hàng tuần
    /// </summary>
    public class WeeklyScheduleResponse
    {
        public List<ShiftScheduleDto> Shifts { get; set; } = new List<ShiftScheduleDto>();
    }

    /// <summary>
    /// DTO cho một ca làm việc
    /// </summary>
    public class ShiftScheduleDto
    {
        public string ShiftName { get; set; } = null!;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public List<DailyScheduleDto> DailySchedules { get; set; } = new List<DailyScheduleDto>();
    }

    /// <summary>
    /// DTO cho lịch làm việc theo ngày
    /// </summary>
    public class DailyScheduleDto
    {
        public DateOnly ShiftDate { get; set; }
        public string DayOfWeek { get; set; } = null!;
        public List<EmployeeScheduleDto> Employees { get; set; } = new List<EmployeeScheduleDto>();
    }

    /// <summary>
    /// DTO cho thông tin nhân viên trong lịch làm việc
    /// </summary>
    public class EmployeeScheduleDto
    {
        public int ScheduleId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = null!;
        public string EmployeeType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request model để thêm lịch làm việc
    /// </summary>
    public class AddScheduleRequest
    {
        [Required(ErrorMessage = "EmployeeId là bắt buộc")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "ShiftDate là bắt buộc")]
        public DateOnly ShiftDate { get; set; }

        [Required(ErrorMessage = "StartTime là bắt buộc")]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "EndTime là bắt buộc")]
        public TimeOnly EndTime { get; set; }

        [StringLength(255, ErrorMessage = "Ghi chú không được vượt quá 255 ký tự")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request model để cập nhật lịch làm việc
    /// </summary>
    public class UpdateScheduleRequest
    {
        public int ScheduleId { get; set; }

        public int? EmployeeId { get; set; }

        /// <summary>
        /// Ngày làm việc - Format: yyyy-MM-dd hoặc yyyyMMdd
        /// </summary>
        public string? ShiftDate { get; set; }

        /// <summary>
        /// Giờ bắt đầu - Format: HH:mm:ss hoặc HH:mm
        /// </summary>
        public string? StartTime { get; set; }

        /// <summary>
        /// Giờ kết thúc - Format: HH:mm:ss hoặc HH:mm
        /// </summary>
        public string? EndTime { get; set; }

        [StringLength(255, ErrorMessage = "Ghi chú không được vượt quá 255 ký tự")]
        public string? Notes { get; set; }

        // Internal parsed values
        internal DateOnly? ParsedShiftDate { get; private set; }
        internal TimeOnly? ParsedStartTime { get; private set; }
        internal TimeOnly? ParsedEndTime { get; private set; }

        /// <summary>
        /// Parse và validate các giá trị string thành DateOnly/TimeOnly
        /// </summary>
        public bool TryParseValues(out string? errorMessage)
        {
            errorMessage = null;

            try
            {
                // Parse ShiftDate
                if (!string.IsNullOrWhiteSpace(ShiftDate))
                {
                    // Try format yyyy-MM-dd
                    if (DateOnly.TryParse(ShiftDate, out var shiftDate))
                    {
                        ParsedShiftDate = shiftDate;
                    }
                    // Try format yyyyMMdd
                    else if (ShiftDate.Length == 8)
                    {
                        int year = int.Parse(ShiftDate.Substring(0, 4));
                        int month = int.Parse(ShiftDate.Substring(4, 2));
                        int day = int.Parse(ShiftDate.Substring(6, 2));
                        ParsedShiftDate = new DateOnly(year, month, day);
                    }
                    else
                    {
                        errorMessage = "ShiftDate phải có format yyyy-MM-dd hoặc yyyyMMdd";
                        return false;
                    }
                }

                // Parse StartTime
                if (!string.IsNullOrWhiteSpace(StartTime))
                {
                    if (TimeOnly.TryParse(StartTime, out var startTime))
                    {
                        ParsedStartTime = startTime;
                    }
                    else
                    {
                        errorMessage = "StartTime phải có format HH:mm:ss hoặc HH:mm";
                        return false;
                    }
                }

                // Parse EndTime
                if (!string.IsNullOrWhiteSpace(EndTime))
                {
                    if (TimeOnly.TryParse(EndTime, out var endTime))
                    {
                        ParsedEndTime = endTime;
                    }
                    else
                    {
                        errorMessage = "EndTime phải có format HH:mm:ss hoặc HH:mm";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi parse dữ liệu: {ex.Message}";
                return false;
            }
        }
    }

    /// <summary>
    /// Response model cho danh sách nhân viên có thể thêm vào ca làm việc
    /// </summary>
    public class AvailableEmployeesResponse
    {
        public List<AvailableEmployeeDto> Employees { get; set; } = new List<AvailableEmployeeDto>();
    }

    /// <summary>
    /// DTO cho nhân viên có thể thêm vào ca
    /// </summary>
    public class AvailableEmployeeDto
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = null!;
        public string EmployeeType { get; set; } = null!;
        public int EmployeeTypeId { get; set; }
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// Request model để kiểm tra nhân viên có sẵn cho ca làm việc
    /// </summary>
    public class CheckAvailableEmployeesRequest
    {
        [Required(ErrorMessage = "ShiftDate là bắt buộc")]
        public DateOnly ShiftDate { get; set; }

        [Required(ErrorMessage = "StartTime là bắt buộc")]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "EndTime là bắt buộc")]
        public TimeOnly EndTime { get; set; }

        /// <summary>
        /// Lọc theo loại nhân viên (optional)
        /// </summary>
        public int? EmployeeTypeId { get; set; }
    }
}