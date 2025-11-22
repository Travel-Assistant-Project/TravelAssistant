using System.Text.Json;

namespace SmartTripApi.Models
{
    public class ItineraryDay
    {
        public int Id { get; set; }
        public int ItineraryId { get; set; }
        public int DayNumber { get; set; }
        public JsonDocument? WeatherInfo { get; set; }
        public DateTime CreatedAt { get; set; }

        public Itinerary? Itinerary { get; set; }
        public List<Activity> Activities { get; set; } = new();
    }
}
