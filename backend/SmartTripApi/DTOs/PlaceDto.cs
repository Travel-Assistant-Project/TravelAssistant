namespace SmartTripApi.DTOs
{
    public class PlaceDto
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
    }
}

