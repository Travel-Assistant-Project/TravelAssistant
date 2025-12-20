using System.Text.Json;

namespace SmartTripApi.Models
{
    public class ActivityTransport
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        
        public string Mode { get; set; } = string.Empty; // walking, driving, transit
        
        public int? DurationMinutes { get; set; }
        public int? DistanceMeters { get; set; }
        
        public string? PolylinePoints { get; set; } // Encoded polyline
        
        public string? DetailsJson { get; set; } // JSONB column
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Activity? Activity { get; set; }
    }
}

