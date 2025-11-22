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
        public DateTime CreatedAt { get; set; }

        public List<Models.Activity> Activities { get; set; } = new();
    }
}
