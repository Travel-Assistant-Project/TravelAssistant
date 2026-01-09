namespace SmartTripApi.Models
{
    public class ItineraryRequestIndex
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string RequestHash { get; set; } = string.Empty;
        public int ItineraryId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
