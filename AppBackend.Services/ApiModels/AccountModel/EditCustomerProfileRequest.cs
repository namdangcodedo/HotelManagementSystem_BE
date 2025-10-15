namespace AppBackend.Services.ApiModels
{
    public class EditCustomerProfileRequest
    {
        public int AccountId { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? IdentityCard { get; set; }
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
