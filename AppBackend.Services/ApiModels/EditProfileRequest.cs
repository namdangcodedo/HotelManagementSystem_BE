using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels
{
    public class EditProfileRequest
    {
        [Required]
        public int AccountId { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        [Phone]
        public string? Phone { get; set; }
        // Add more fields as needed
    }
}

