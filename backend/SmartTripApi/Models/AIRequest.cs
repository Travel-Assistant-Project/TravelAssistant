using System.Text.Json;

namespace SmartTripApi.Models
{
    public class AIRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? ItineraryId { get; set; }
        public JsonDocument RequestPayload { get; set; } = null!;
        public JsonDocument? AiResponse { get; set; }
        public string Status { get; set; } = "completed";
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
        public Itinerary? Itinerary { get; set; }
    }
}
