using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.Services.ApiModels.AttendanceModel
{
    public class PostAttendInfoRequest
    {
        public int AttendInfoId { get; set; }

        [Required(ErrorMessage = "Employee Id là bắt buộc")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Year là bắt buộc")]
        public int Year { get; set; }

        public int? TotalLeaveRequest { get; set; }

        public int? RemainLeaveRequest { get; set; }

        public int? UsedLeaveRequest { get; set; }

        public int? OverLeaveDay { get; set; }
    }

    public class PostAttendInfosRequest
    {
        public IEnumerable<PostAttendInfoRequest> AttendInfos { get; set; } = null!;
    }

}

