using System.ComponentModel.DataAnnotations;

namespace SmartTripApi.DTOs
{
    public class EventSearchRequestDto
    {
        [Required]
        public string Location { get; set; } = string.Empty;
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        public string? EventType { get; set; } // music, sports, arts, family etc.
        
        public int? Radius { get; set; } = 50; // km cinsinden radius
        
        public int? Limit { get; set; } = 20;
    }

    public class EventDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ImageUrl { get; set; }
        public EventVenueDto? Venue { get; set; }
        public EventPriceDto? PriceRange { get; set; }
        public string? EventType { get; set; }
        public string? Genre { get; set; }
        public string? TicketUrl { get; set; }
        public string Source { get; set; } = string.Empty; // ticketmaster, eventbrite etc.
    }

    public class EventVenueDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class EventPriceDto
    {
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public string Currency { get; set; } = "USD";
    }

    public class EventSearchResponseDto
    {
        public List<EventDto> Events { get; set; } = new();
        public int TotalCount { get; set; }
        public string? SearchLocation { get; set; }
        public DateTime SearchStartDate { get; set; }
        public DateTime SearchEndDate { get; set; }
    }
}
