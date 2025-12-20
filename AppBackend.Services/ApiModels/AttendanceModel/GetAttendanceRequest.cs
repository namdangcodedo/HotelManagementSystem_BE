using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.ApiModels.AttendanceModel
{
    public class GetAttendanceRequest : PagedRequestDto
    {
        public string? EmployeeName { get; set; }
        public int? EmployeeId { get; set; }
        public int? Day { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }

        public string? Status { get; set; }

        public DateTime? workDate { get; set; }

    }
}

