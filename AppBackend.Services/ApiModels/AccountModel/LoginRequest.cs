using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels
{
    public class LoginRequest
    {
        /// <summary>
        /// Email hoặc Username để đăng nhập
        /// </summary>
        [Required(ErrorMessage = "Email hoặc Username là bắt buộc")]
        public string Email { get; set; } = null!;
        
        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string Password { get; set; } = null!;
    }
}
