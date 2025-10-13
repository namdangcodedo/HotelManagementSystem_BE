using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AppBackend.Extensions;

public static class AuthConfig
{
    /// <summary>
    /// Register default authentication with JWT and Cookie.
    /// </summary>
    public static IServiceCollection AddDefaultAuth(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["Jwt:Issuer"],
                ValidAudience = config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]))
            };
        })
        .AddCookie("Cookies");

        return services;
    }

    /// <summary>
    /// Register Google authentication (optional).
    /// This should be called only if you need Google login.
    /// </summary>
    public static IServiceCollection AddGoogleAuth(this IServiceCollection services, IConfiguration config)
    {
        var clientId = config["Google:ClientId"];
        var clientSecret = config["Google:ClientSecret"];

        if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
        {
            services.AddAuthentication()
                .AddGoogle(googleOptions =>
                {
                    googleOptions.ClientId = clientId;
                    googleOptions.ClientSecret = clientSecret;
                    googleOptions.CallbackPath = "/signin-google";
                });
        }

        return services;
    }
}
