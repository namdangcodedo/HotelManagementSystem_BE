using AppBackend.BusinessObjects.Dtos;
using AppBackend.Services.ApiModels;

namespace AppBackend.Services.ApiModels.SalaryModel
{
    public class GetSalaryInfoRequest : PagedRequestDto
    {
        public int? EmployeeId { get; set; }
        public int? Year { get; set; }
    }
}