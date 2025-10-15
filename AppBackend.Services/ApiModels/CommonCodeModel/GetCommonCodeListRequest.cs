using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.ApiModels.CommonCodeModel
{
    public class GetCommonCodeListRequest : PagedRequestDto
    {
        public string? CodeType { get; set; }
        public bool? IsActive { get; set; }
    }
}

