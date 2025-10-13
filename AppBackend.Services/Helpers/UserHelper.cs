using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AppBackend.BusinessObjects.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AppBackend.Services.ServicesHelpers
{
    public class UserHelper
    {
        private readonly IConfiguration _configuration;

        public UserHelper(IConfiguration configuration)
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

        public string CreateToken(User user)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            if (key.Length < 32)
                throw new Exception("JWT Key must be at least 256 bits (32 chars).");

            var securityKey = new SymmetricSecurityKey(key);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? ""),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

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
