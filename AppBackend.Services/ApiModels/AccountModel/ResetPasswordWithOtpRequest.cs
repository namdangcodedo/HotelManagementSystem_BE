namespace AppBackend.Services.ApiModels
{
    public class ResetPasswordWithOtpRequest
    {
        public string Email { get; set; }
        public string Otp { get; set; }
        public string NewPassword { get; set; }
    }
}

