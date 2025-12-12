using System;
using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.AttendanceModel
{
    public class PostAttendanceRequest
    {
        public int AttendanceId { get; set; }

        [Required(ErrorMessage = "Employee Id là bắt buộc")]
        public int EmployeeId { get; set; }
        public string? DeviceEmployeeId { get; set; }
        public decimal? OvertimeHours { get; set; }

        public DateTime CheckIn { get; set; }

        public DateTime? CheckOut { get; set; }
        public string? Notes { get; set; }

        public DateTime? CreatedAt { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public string? Status { get; set; }
        public string? IsApproved { get; set; }
    }

    public class PostAttendancesRequest
    {
        public IEnumerable<PostAttendanceRequest> Attendances { get; set; } = null!;
    }

}

