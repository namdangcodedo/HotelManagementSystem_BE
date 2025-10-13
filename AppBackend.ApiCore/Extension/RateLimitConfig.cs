using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace AppBackend.Extensions
{
    public static class RateLimitConfig
    {
        public static IServiceCollection AddRateLimitConfig(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                // Policy for Register
                options.AddPolicy("RegisterPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 3,       // Max 3 requests
                            Window = TimeSpan.FromSeconds(60),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 1
                        }));

                // Policy for Login
                options.AddPolicy("LoginPolicy", httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 5,       // Max 5 requests
                            Window = TimeSpan.FromSeconds(60),
                            SegmentsPerWindow = 3,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 2
                        }));

                // Policy for Global API calls
                options.AddPolicy("GlobalPolicy", httpContext =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 10,       // Max 10 tokens
                            TokensPerPeriod = 2,   // Refill 2 tokens
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 2,
                            AutoReplenishment = true
                        }));
            });

            return services;
        }
    }
}
