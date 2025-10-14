namespace AppBackend.ApiCore.Extensions
{
    public static class CacheConfig
    {
        public static IServiceCollection AddCacheConfig(this IServiceCollection services)
        {
            // In-memory caching
            services.AddMemoryCache();

            // Distributed cache (optional, can use Redis in the future)
            services.AddDistributedMemoryCache();

            return services;
        }
    }
}