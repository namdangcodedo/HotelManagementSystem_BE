using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels
{
    public class GoogleLoginRequest
    {
        [Required]
        public string GoogleToken { get; set; } = null!;
    }
}

