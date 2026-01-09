using StackExchange.Redis;

namespace SmartTripApi.Services.RoutePlanning.Caching
{
    public class RedisRouteCacheService : IRouteCacheService
    {
        private readonly IDatabase _db;

        public RedisRouteCacheService(IConnectionMultiplexer mux)
        {
            _db = mux.GetDatabase();
        }

        private static string CacheRedisKey(string cacheKey) => $"route:cache:{cacheKey}";
        private static string LockRedisKey(string cacheKey) => $"route:lock:{cacheKey}";

        public async Task<int?> GetItineraryIdAsync(string cacheKey)
        {
            var value = await _db.StringGetAsync(CacheRedisKey(cacheKey));
            if (!value.HasValue) return null;
            return int.TryParse(value.ToString(), out var id) ? id : null;
        }

        public Task SetItineraryIdAsync(string cacheKey, int itineraryId, TimeSpan ttl)
            => _db.StringSetAsync(CacheRedisKey(cacheKey), itineraryId.ToString(), ttl);

        public async Task<string?> TryAcquireLockAsync(string cacheKey, TimeSpan ttl)
        {
            var token = Guid.NewGuid().ToString("N");
            var ok = await _db.StringSetAsync(LockRedisKey(cacheKey), token, ttl, When.NotExists);
            return ok ? token : null;
        }

        public async Task ReleaseLockAsync(string cacheKey, string token)
        {
            // lock sahibi değilse silmesin diye LUA
            var script = @"
if redis.call('GET', KEYS[1]) == ARGV[1]
then
  return redis.call('DEL', KEYS[1])
else
  return 0
end";
            await _db.ScriptEvaluateAsync(script,
                keys: new RedisKey[] { LockRedisKey(cacheKey) },
                values: new RedisValue[] { token });
        }

        public async Task<int?> WaitForItineraryIdAsync(string cacheKey, TimeSpan timeout, TimeSpan pollInterval)
        {
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < timeout)
            {
                var id = await GetItineraryIdAsync(cacheKey);
                if (id.HasValue) return id;
                await Task.Delay(pollInterval);
            }
            return null;
        }
    }
}
