using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.CommonCodeModel
{
    public class AddCommonCodeRequest
    {
        [Required(ErrorMessage = "Loại mã là bắt buộc")]
        public string CodeType { get; set; } = null!;

        [Required(ErrorMessage = "Giá trị mã là bắt buộc")]
        public string CodeValue { get; set; } = null!;

        [Required(ErrorMessage = "Tên mã là bắt buộc")]
        public string CodeName { get; set; } = null!;

        public string? Description { get; set; }
        public int? DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

