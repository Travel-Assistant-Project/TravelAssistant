namespace SmartTripApi.Models
{
    public class PlacePhoto
    {
        public int Id { get; set; }
        public int PlaceId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public Place? Place { get; set; }
    }
}
