using AppBackend.BusinessObjects.AppSettings;
using AppBackend.Services.ApiModels;
using AppBackend.Services.Helpers;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.BusinessObjects.Models;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppBackend.Services.Authentication
{
    public class GoogleLoginService : IGoogleLoginService
    {
        private readonly GoogleAuthSettings _settings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AccountHelper _accountHelper;
        private readonly CacheHelper _cacheHelper;
        private readonly HttpClient _httpClient;

        public GoogleLoginService(IUnitOfWork unitOfWork, AccountHelper accountHelper, CacheHelper cacheHelper, IOptions<GoogleAuthSettings> options)
        {
            _unitOfWork = unitOfWork;
            _accountHelper = accountHelper;
            _cacheHelper = cacheHelper;
            _settings = options.Value;
            _httpClient = new HttpClient();
        }

        public string GetGoogleLoginUrl()
        {
            return $"https://accounts.google.com/o/oauth2/v2/auth?client_id={_settings.ClientId}&redirect_uri={_settings.RedirectUri}&response_type=code&scope=openid%20email%20profile";
        }

        public async Task<GoogleUserInfo> GetUserInfoFromCodeAsync(string code)
        {
            // Exchange code for access token
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("client_id", _settings.ClientId),
                    new KeyValuePair<string, string>("client_secret", _settings.ClientSecret),
                    new KeyValuePair<string, string>("redirect_uri", _settings.RedirectUri),
                    new KeyValuePair<string, string>("grant_type", "authorization_code")
                })
            };
            var tokenResponse = await _httpClient.SendAsync(tokenRequest);
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            using var tokenDoc = JsonDocument.Parse(tokenJson);
            var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();

            // Get user info
            var userInfoResponse = await _httpClient.GetAsync($"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}");
            userInfoResponse.EnsureSuccessStatusCode();
            var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(userInfoJson);
            return userInfo ?? new GoogleUserInfo();
        }
    }
}
