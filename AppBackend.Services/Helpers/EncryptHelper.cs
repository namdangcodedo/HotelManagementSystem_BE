using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AppBackend.BusinessObjects.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AppBackend.BusinessObjects.Models;

namespace AppBackend.Services.Helpers
{
    public class EncryptHelper
    {
        private readonly IConfiguration _configuration;
        private static readonly string key = "bWluZHN0b25lX2lzX3RoZV9iZXN0X3NlY3JldF9rZXk="; // Base64 for "mindstone_is_the_best_secret_key"

        public EncryptHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string DecryptString(string cipherText, string iv)
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(key);
            aes.IV = Convert.FromBase64String(iv);

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }

    }
}
