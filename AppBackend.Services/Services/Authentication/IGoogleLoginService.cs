using System.Threading.Tasks;
using AppBackend.Services.ApiModels;

namespace AppBackend.Services.Authentication
{
    public interface IGoogleLoginService
    {
        string GetGoogleLoginUrl();
        Task<GoogleUserInfo> GetUserInfoFromCodeAsync(string code);
    }
}
