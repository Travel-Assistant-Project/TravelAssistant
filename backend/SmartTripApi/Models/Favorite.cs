namespace SmartTripApi.Models
{
    public class Favorite
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? PlaceId { get; set; }
        public int? ItineraryId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Place? Place { get; set; }
        public Itinerary? Itinerary { get; set; }
    }
}

