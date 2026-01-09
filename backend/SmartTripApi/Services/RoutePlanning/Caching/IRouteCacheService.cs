namespace SmartTripApi.Services.RoutePlanning.Caching
{
    public interface IRouteCacheService
    {
        Task<int?> GetItineraryIdAsync(string cacheKey);
        Task SetItineraryIdAsync(string cacheKey, int itineraryId, TimeSpan ttl);

        // single-flight lock
        Task<string?> TryAcquireLockAsync(string cacheKey, TimeSpan ttl);
        Task ReleaseLockAsync(string cacheKey, string token);

        // lock alamayanlar bekler -> cache'e düşünce alır
        Task<int?> WaitForItineraryIdAsync(string cacheKey, TimeSpan timeout, TimeSpan pollInterval);
    }
}
