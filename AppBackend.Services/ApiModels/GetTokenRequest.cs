namespace AppBackend.Services.ApiModels
{
    public class GetTokenRequest
    {
        public int AccountId { get; set; }
        public string RefreshToken { get; set; }
    }
}

