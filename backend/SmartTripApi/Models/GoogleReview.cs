namespace SmartTripApi.Models
{
    public class GoogleReview
    {
        public int Id { get; set; }
        public int PlaceId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public DateTime? ReviewTime { get; set; }
        public DateTime CreatedAt { get; set; }

        public Place? Place { get; set; }
    }
}
