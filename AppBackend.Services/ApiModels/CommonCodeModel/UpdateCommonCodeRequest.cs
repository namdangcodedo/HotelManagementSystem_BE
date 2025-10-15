namespace AppBackend.Services.ApiModels.CommonCodeModel
{
    public class UpdateCommonCodeRequest
    {
        public int CodeId { get; set; }
        public string? CodeType { get; set; }
        public string? CodeValue { get; set; }
        public string? CodeName { get; set; }
        public string? Description { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
    }
}

