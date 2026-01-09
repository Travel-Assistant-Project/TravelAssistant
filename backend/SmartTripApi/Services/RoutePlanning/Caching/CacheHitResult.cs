namespace SmartTripApi.Services.RoutePlanning.Caching
{
    public record CacheHitResult(bool Hit, int? ItineraryId);
}
