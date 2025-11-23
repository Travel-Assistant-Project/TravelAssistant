namespace SmartTripApi.Models
{
    public class Place
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ThemeTypeEnum? Category { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? GoogleMapsUrl { get; set; }
        public decimal? GoogleRating { get; set; }
        
        // Google Places API fields
        public string? GooglePlaceId { get; set; }
        public string? FormattedAddress { get; set; }
        public int? UserRatingsTotal { get; set; }
        public int? PriceLevel { get; set; }
        public string? OpeningHours { get; set; } // JSON string
        public string[]? PhotoUrls { get; set; }
        
        public DateTime CreatedAt { get; set; }

        public List<Models.Activity> Activities { get; set; } = new();
        public List<PlacePhoto> PlacePhotos { get; set; } = new();
    }
}
