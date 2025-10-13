using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.RateLimiting;
using AppBackend.Services.RateLimiting;
using AppBackend.Services.ApiModels;

namespace AppBackend.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RateLimitAttribute : Attribute, IAsyncActionFilter
    {
        public int PermitLimit { get; }
        public int WindowSeconds { get; }
        public int QueueLimit { get; }
        public string Strategy { get; }

        public RateLimitAttribute(int permitLimit, int windowSeconds, int queueLimit = 0, string strategy = "fixed")
        {
            PermitLimit = permitLimit;
            WindowSeconds = windowSeconds;
            QueueLimit = queueLimit;
            Strategy = strategy.ToLower();
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var store = context.HttpContext.RequestServices.GetRequiredService<RateLimiterStore>();
            var key = $"{Strategy}:{context.HttpContext.Connection.RemoteIpAddress}:{context.ActionDescriptor.DisplayName}";

            var limiter = store.GetOrCreate(key, () => CreateLimiter());

            var lease = limiter.AcquireAsync(1).AsTask();

            if (lease.IsCompletedSuccessfully && lease.Result.IsAcquired)
            {
                await next();
            }
            else
            {
                var result = new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "RATE_LIMIT_EXCEEDED",
                    Message = $"Too many requests. Limit = {PermitLimit} requests per {WindowSeconds} seconds.",
                    Data = null,
                    StatusCode = StatusCodes.Status429TooManyRequests
                };

                context.HttpContext.Response.Headers["Retry-After"] = WindowSeconds.ToString();

                context.Result = new JsonResult(result)
                {
                    StatusCode = StatusCodes.Status429TooManyRequests
                };
            }
        }

        private RateLimiter CreateLimiter()
        {
            return Strategy switch
            {
                "fixed" => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
                {
                    PermitLimit = PermitLimit,
                    Window = TimeSpan.FromSeconds(WindowSeconds),
                    QueueLimit = QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }),
                "sliding" => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = PermitLimit,
                    Window = TimeSpan.FromSeconds(WindowSeconds),
                    SegmentsPerWindow = 3,
                    QueueLimit = QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }),
                "token" => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                {
                    TokenLimit = PermitLimit,
                    TokensPerPeriod = PermitLimit,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(WindowSeconds),
                    AutoReplenishment = true,
                    QueueLimit = QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }),
                "concurrency" => new ConcurrencyLimiter(new ConcurrencyLimiterOptions
                {
                    PermitLimit = PermitLimit,
                    QueueLimit = QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }),
                _ => throw new ArgumentException($"Invalid strategy: {Strategy}")
            };
        }
    }
}
