using StackExchange.Redis;
using WhearApp.Infrastructure.Caching;
using WhearApp.Infrastructure.Caching.Providers;

namespace WhearApp.WebApi.Extensions.DI;

public static class CachingServiceExtensions
{
    public static void AddCacheService(
        this IServiceCollection services,
        Action<CacheOptions> configureOptions)
    {
        var options = new CacheOptions();
        configureOptions(options);
        services.AddSingleton(options);

        if (options.CacheType == CacheType.Redis)
        {
            if (string.IsNullOrEmpty(options.ConnectionString))
                throw new ArgumentException("Redis connection string is required for Redis cache type.");

            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                try
                {
                    var configuration = ConfigurationOptions.Parse(options.ConnectionString);
                    configuration.AbortOnConnectFail = false;
                    configuration.ConnectRetry = 3;
                    configuration.ConnectTimeout = 5000;
                    configuration.SyncTimeout = 5000;
                    configuration.ReconnectRetryPolicy = new ExponentialRetry(1000);
                    
                    return ConnectionMultiplexer.Connect(configuration);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to connect to Redis: {ex.Message}", ex);
                }
            });

            services.AddSingleton<IRedisDistributedCache, RedisDistributedCache>();
            // services.AddStackExchangeRedisCache(redisOptions =>
            // {
            //     redisOptions.InstanceName = options.InstanceName ?? "AppCache";
            // });
        }
        else
        {
            throw new NotImplementedException("Memory cache is not implemented yet.");
        }
    }
}