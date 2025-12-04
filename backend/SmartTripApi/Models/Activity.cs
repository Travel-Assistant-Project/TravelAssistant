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

        // Transport info from previous activity to this one
        public string? TravelFromPreviousMode { get; set; } // "transit", "walking", "driving"
        public int? TravelFromPreviousDurationMinutes { get; set; }
        public int? TravelFromPreviousDistanceMeters { get; set; }
        public string? TravelFromPreviousDetailsJson { get; set; }
        public string? TravelFromPreviousPolyline { get; set; } // Encoded polyline points

        // New Transport Relation (Preferred Way)
        public ActivityTransport? Transport { get; set; }

        public ItineraryDay? ItineraryDay { get; set; }
        public Place? Place { get; set; }
    }
}
