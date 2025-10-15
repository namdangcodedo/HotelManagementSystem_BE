namespace AppBackend.Services.ApiModels
{
    public class EditProfileRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        // Add more fields as needed
    }
}

