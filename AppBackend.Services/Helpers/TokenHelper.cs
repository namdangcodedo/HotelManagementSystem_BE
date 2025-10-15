using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AppBackend.Services.Helpers
{
    public static class TokenHelper
    {
        public static int? GetAccountIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var accountIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (accountIdClaim != null && int.TryParse(accountIdClaim.Value, out int accountId))
                return accountId;
            return null;
        }
    }
}

