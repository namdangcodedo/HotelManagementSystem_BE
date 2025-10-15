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
    public class AccountHelper
    {
        private readonly IConfiguration _configuration;

        public AccountHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        public bool VerifyPassword(string inputPassword, string storedPasswordHash)
        {
            return BCrypt.Net.BCrypt.Verify(inputPassword, storedPasswordHash);
        }

        public string CreateToken(Account account, List<string> roleNames)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty);
            if (key.Length < 32)
                throw new Exception("JWT Key must be at least 256 bits (32 chars).");

            var securityKey = new SymmetricSecurityKey(key);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                new Claim(ClaimTypes.Name, account.Username ?? "")
            };

            if (roleNames != null && roleNames.Count > 0)
            {
                foreach (var roleName in roleNames)
                {
                    claims.Add(new Claim(ClaimTypes.Role, roleName));
                }
            }
            else
            {
                claims.Add(new Claim(ClaimTypes.Role, RoleEnums.User.ToString()));
            }

            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            var now = DateTime.UtcNow;
            var expiresMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "30");

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(expiresMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        public DateTime GetRefreshTokenExpiry()
        {
            var days = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
            return DateTime.UtcNow.AddDays(days);
        }
    }
}
