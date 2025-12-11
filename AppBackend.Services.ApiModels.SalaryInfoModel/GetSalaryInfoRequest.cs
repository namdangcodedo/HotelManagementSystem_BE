using AppBackend.Services.ApiModels;
namespace AppBackend.Services.ApiModels.SalaryInfoModel
{
    public class GetSalaryInfoRequest : PagedRequestDto
    {
        public int? EmployeeId { get; set; }
        public int? Year { get; set; }
    }
}