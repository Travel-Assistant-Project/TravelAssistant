namespace SmartTripApi.Models
{
    public class Activity
    {
        public int Id { get; set; }
        public int? ItineraryDayId { get; set; }
        public int? PlaceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Reason { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string[]? ImageUrls { get; set; }
        public DateTime CreatedAt { get; set; }

        public ItineraryDay? ItineraryDay { get; set; }
        public Place? Place { get; set; }
    }
}
