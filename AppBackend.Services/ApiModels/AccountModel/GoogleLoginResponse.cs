namespace AppBackend.Services.ApiModels.AccountModel
{
    public class GoogleLoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
    }
}
