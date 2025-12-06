using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.ApiModels.EmployeeModel
{
    public class GetEmployeeRequest : PagedRequestDto
    {
        public int? EmployeeTypeId { get; set; }
        public bool? IsActive { get; set; }
    }
}

