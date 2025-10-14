using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = null!;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = null!;
    }
}

