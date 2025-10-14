using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels
{
    public class LogoutRequest
    {
        [Required]
        public int AccountId { get; set; }
    }
}

