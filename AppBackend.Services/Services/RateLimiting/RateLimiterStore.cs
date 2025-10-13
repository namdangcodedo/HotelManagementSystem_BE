using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace AppBackend.Services.RateLimiting
{
    public class RateLimiterStore
    {
        private readonly ConcurrentDictionary<string, RateLimiter> _limiters = new();

        public RateLimiter GetOrCreate(string key, Func<RateLimiter> factory)
        {
            return _limiters.GetOrAdd(key, _ => factory());
        }
    }
}