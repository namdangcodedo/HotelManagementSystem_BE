using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.ApiModels.AttendanceModel
{
    public class GetAttendanceRequest : PagedRequestDto
    {
        public int? EmployeeId { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }

    }
}

