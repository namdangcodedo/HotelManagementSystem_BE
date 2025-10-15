using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.ApiModels.EmployeeModel
{
    public class GetEmployeeListRequest : PagedRequestDto
    {
        public int? EmployeeTypeId { get; set; }
        public bool? IsActive { get; set; }
    }
}

