namespace AppBackend.BusinessObjects.AppSettings
{
    public class GoogleAuthSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string JavascriptOrigin { get; set; } = string.Empty;
    }
}

