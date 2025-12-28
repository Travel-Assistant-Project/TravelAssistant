namespace SmartTripApi.DTOs
{
    public class PlaceDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string Location { get; set; } = string.Empty;
        public List<string>? ImageUrls { get; set; }
        public decimal? GoogleRating { get; set; }
        public int? UserRatingsTotal { get; set; }
        public int? PriceLevel { get; set; }
        public string? GoogleMapsUrl { get; set; }
        public string? FormattedAddress { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public List<PlaceReviewDto> GoogleReviews { get; set; } = new();
    }

    public class PlaceReviewDto
    {
        public string AuthorName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public DateTime? ReviewTime { get; set; }
    }
}
