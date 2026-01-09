using System.Text.Json;
using System.Text.Json.Serialization;
using SmartTripApi.DTOs;

namespace SmartTripApi.Services.Events
{
    public interface IEventService
    {
        Task<EventSearchResponseDto> SearchEventsAsync(EventSearchRequestDto request);
    }

    public class TicketmasterService : IEventService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TicketmasterService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://app.ticketmaster.com/discovery/v2";

        public TicketmasterService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<TicketmasterService> logger)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Increase timeout for API calls
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["TicketmasterApi:ApiKey"] 
                ?? throw new InvalidOperationException("Ticketmaster API key not found");
        }

        public async Task<EventSearchResponseDto> SearchEventsAsync(EventSearchRequestDto request)
        {
            try
            {
                // Determine country code from location
                var countryCode = GetCountryCodeFromLocation(request.Location);
                
                var queryParams = new List<string>
                {
                    $"apikey={_apiKey}",
                    $"city={Uri.EscapeDataString(request.Location)}",
                    $"size={request.Limit}",
                    $"countryCode={countryCode}"
                };

                if (request.StartDate != default(DateTime))
                {
                    var startDate = request.StartDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    queryParams.Add($"startDateTime={startDate}");
                }

                if (request.EndDate != default(DateTime))
                {
                    var endDate = request.EndDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    queryParams.Add($"endDateTime={endDate}");
                }

                if (!string.IsNullOrEmpty(request.EventType))
                {
                    queryParams.Add($"classificationName={Uri.EscapeDataString(request.EventType)}");
                }

                var url = $"{_baseUrl}/events.json?{string.Join("&", queryParams)}";
                
                _logger.LogInformation("Searching events with URL: {Url}", url.Replace(_apiKey, "***"));

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                
                // DEBUG: Log the raw response
                _logger.LogInformation("Ticketmaster API raw response: {Content}", content.Substring(0, Math.Min(500, content.Length)));
                
                var ticketmasterResponse = JsonSerializer.Deserialize<TicketmasterResponse>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // DEBUG: Log parsed response
                _logger.LogInformation("Found {EventCount} events from Ticketmaster", 
                    ticketmasterResponse?.Embedded?.Events?.Count ?? 0);

                var eventDtos = new List<EventDto>();
                
                if (ticketmasterResponse?.Embedded?.Events != null)
                {
                    foreach (var tmEvent in ticketmasterResponse.Embedded.Events)
                    {
                        var eventDto = MapToEventDto(tmEvent);
                        eventDtos.Add(eventDto);
                    }
                }

                _logger.LogInformation("Found {EventCount} events for location: {Location}", 
                    eventDtos.Count, request.Location);

                // DEBUG: Log each event with its image URL
                foreach (var evt in eventDtos)
                {
                    _logger.LogInformation("Event: {EventName}, ImageUrl: {ImageUrl}", evt.Name, evt.ImageUrl);
                }

                return new EventSearchResponseDto
                {
                    Events = eventDtos,
                    TotalCount = ticketmasterResponse?.Page?.TotalElements ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events from Ticketmaster");
                return new EventSearchResponseDto
                {
                    Events = new List<EventDto>(),
                    TotalCount = 0
                };
            }
        }

        private EventDto MapToEventDto(TicketmasterEvent tmEvent)
        {
            var eventDto = new EventDto
            {
                Id = tmEvent.Id ?? Guid.NewGuid().ToString(),
                Name = tmEvent.Name ?? "Unnamed Event",
                Description = tmEvent.Info,
                Source = "Ticketmaster"
            };

            // Dates
            if (tmEvent.Dates?.Start != null)
            {
                if (DateTime.TryParse(tmEvent.Dates.Start.DateTime ?? tmEvent.Dates.Start.LocalDate, out var startDate))
                {
                    eventDto.StartDate = startDate;
                }
            }

            // Use category-based placeholder images instead of problematic Ticketmaster images
            var eventType = tmEvent.Classifications?.FirstOrDefault()?.Segment?.Name?.ToLower() ?? "music";
            
            _logger.LogInformation("Event {EventName} assigned category image for type: {EventType}", 
                tmEvent.Name, eventType);

            // Venue
            if (tmEvent.Embedded?.Venues?.Any() == true)
            {
                var venue = tmEvent.Embedded.Venues.First();
                eventDto.Venue = new EventVenueDto
                {
                    Name = venue.Name ?? string.Empty,
                    Address = venue.Address?.Line1,
                    City = venue.City?.Name,
                    Country = venue.Country?.Name
                };
            }

            // Price Range
            if (tmEvent.PriceRanges?.Any() == true)
            {
                var priceRange = tmEvent.PriceRanges.First();
                eventDto.PriceRange = new EventPriceDto
                {
                    Min = priceRange.Min,
                    Max = priceRange.Max,
                    Currency = priceRange.Currency ?? "TRY"
                };
            }

            // Event Type/Classification
            if (tmEvent.Classifications?.Any() == true)
            {
                var classification = tmEvent.Classifications.First();
                eventDto.EventType = classification.Segment?.Name ?? classification.Genre?.Name;
                eventDto.Genre = classification.Genre?.Name;
            }

            // Ticket URL
            eventDto.TicketUrl = tmEvent.Url;

            return eventDto;
        }


        private string GetCountryCodeFromLocation(string location)
        {
            // Extract country code from location string
            var locationLower = location.ToLower();
            
            // Common city/country patterns
            if (locationLower.Contains("uk") || locationLower.Contains("london") || 
                locationLower.Contains("manchester") || locationLower.Contains("birmingham") || 
                locationLower.Contains("england") || locationLower.Contains("scotland") || 
                locationLower.Contains("wales") || locationLower.Contains("northern ireland"))
            {
                return "GB"; // United Kingdom
            }
            
            if (locationLower.Contains("usa") || locationLower.Contains("us") || 
                locationLower.Contains("new york") || locationLower.Contains("los angeles") ||
                locationLower.Contains("chicago") || locationLower.Contains("miami") || 
                locationLower.Contains("united states"))
            {
                return "US"; // United States
            }
            
            if (locationLower.Contains("canada") || locationLower.Contains("toronto") || 
                locationLower.Contains("vancouver") || locationLower.Contains("montreal"))
            {
                return "CA"; // Canada
            }
            
            if (locationLower.Contains("germany") || locationLower.Contains("berlin") || 
                locationLower.Contains("munich") || locationLower.Contains("hamburg"))
            {
                return "DE"; // Germany
            }
            
            if (locationLower.Contains("france") || locationLower.Contains("paris") || 
                locationLower.Contains("lyon") || locationLower.Contains("marseille"))
            {
                return "FR"; // France
            }
            
            if (locationLower.Contains("spain") || locationLower.Contains("madrid") || 
                locationLower.Contains("barcelona") || locationLower.Contains("valencia"))
            {
                return "ES"; // Spain
            }
            
            if (locationLower.Contains("italy") || locationLower.Contains("rome") || 
                locationLower.Contains("milan") || locationLower.Contains("naples"))
            {
                return "IT"; // Italy
            }
            
            if (locationLower.Contains("netherlands") || locationLower.Contains("amsterdam") || 
                locationLower.Contains("rotterdam") || locationLower.Contains("holland"))
            {
                return "NL"; // Netherlands
            }
            
            if (locationLower.Contains("australia") || locationLower.Contains("sydney") || 
                locationLower.Contains("melbourne") || locationLower.Contains("brisbane"))
            {
                return "AU"; // Australia
            }
            
            // More European countries
            if (locationLower.Contains("belgium") || locationLower.Contains("brussels") || 
                locationLower.Contains("antwerp") || locationLower.Contains("ghent"))
            {
                return "BE"; // Belgium
            }
            
            if (locationLower.Contains("austria") || locationLower.Contains("vienna") || 
                locationLower.Contains("salzburg") || locationLower.Contains("innsbruck"))
            {
                return "AT"; // Austria
            }
            
            if (locationLower.Contains("switzerland") || locationLower.Contains("zurich") || 
                locationLower.Contains("geneva") || locationLower.Contains("basel"))
            {
                return "CH"; // Switzerland
            }
            
            if (locationLower.Contains("sweden") || locationLower.Contains("stockholm") || 
                locationLower.Contains("gothenburg") || locationLower.Contains("malmö"))
            {
                return "SE"; // Sweden
            }
            
            if (locationLower.Contains("norway") || locationLower.Contains("oslo") || 
                locationLower.Contains("bergen") || locationLower.Contains("trondheim"))
            {
                return "NO"; // Norway
            }
            
            if (locationLower.Contains("denmark") || locationLower.Contains("copenhagen") || 
                locationLower.Contains("aarhus") || locationLower.Contains("odense"))
            {
                return "DK"; // Denmark
            }
            
            if (locationLower.Contains("poland") || locationLower.Contains("warsaw") || 
                locationLower.Contains("krakow") || locationLower.Contains("gdansk"))
            {
                return "PL"; // Poland
            }
            
            if (locationLower.Contains("czech") || locationLower.Contains("prague") || 
                locationLower.Contains("brno") || locationLower.Contains("ostrava"))
            {
                return "CZ"; // Czech Republic
            }
            
            if (locationLower.Contains("hungary") || locationLower.Contains("budapest") || 
                locationLower.Contains("debrecen") || locationLower.Contains("szeged"))
            {
                return "HU"; // Hungary
            }
            
            if (locationLower.Contains("portugal") || locationLower.Contains("lisbon") || 
                locationLower.Contains("porto") || locationLower.Contains("braga"))
            {
                return "PT"; // Portugal
            }
            
            // More countries worldwide
            if (locationLower.Contains("japan") || locationLower.Contains("tokyo") || 
                locationLower.Contains("osaka") || locationLower.Contains("kyoto"))
            {
                return "JP"; // Japan
            }
            
            if (locationLower.Contains("south korea") || locationLower.Contains("korea") || 
                locationLower.Contains("seoul") || locationLower.Contains("busan"))
            {
                return "KR"; // South Korea
            }
            
            if (locationLower.Contains("singapore"))
            {
                return "SG"; // Singapore
            }
            
            if (locationLower.Contains("new zealand") || locationLower.Contains("auckland") || 
                locationLower.Contains("wellington") || locationLower.Contains("christchurch"))
            {
                return "NZ"; // New Zealand
            }
            
            if (locationLower.Contains("mexico") || locationLower.Contains("mexico city") || 
                locationLower.Contains("guadalajara") || locationLower.Contains("monterrey"))
            {
                return "MX"; // Mexico
            }
            
            if (locationLower.Contains("brazil") || locationLower.Contains("sao paulo") || 
                locationLower.Contains("rio de janeiro") || locationLower.Contains("brasilia"))
            {
                return "BR"; // Brazil
            }
            
            if (locationLower.Contains("argentina") || locationLower.Contains("buenos aires") || 
                locationLower.Contains("cordoba") || locationLower.Contains("rosario"))
            {
                return "AR"; // Argentina
            }
            
            if (locationLower.Contains("india") || locationLower.Contains("mumbai") || 
                locationLower.Contains("delhi") || locationLower.Contains("bangalore") ||
                locationLower.Contains("chennai") || locationLower.Contains("kolkata"))
            {
                return "IN"; // India
            }
            
            if (locationLower.Contains("russia") || locationLower.Contains("moscow") || 
                locationLower.Contains("st petersburg") || locationLower.Contains("saint petersburg"))
            {
                return "RU"; // Russia
            }
            
            if (locationLower.Contains("south africa") || locationLower.Contains("cape town") || 
                locationLower.Contains("johannesburg") || locationLower.Contains("durban"))
            {
                return "ZA"; // South Africa
            }
            
            // Turkey and Turkish cities
            if (locationLower.Contains("turkey") || locationLower.Contains("türkiye") || 
                locationLower.Contains("istanbul") || locationLower.Contains("ankara") ||
                locationLower.Contains("izmir") || locationLower.Contains("bursa") ||
                locationLower.Contains("antalya") || locationLower.Contains("gaziantep"))
            {
                return "TR"; // Turkey
            }
            
            // Default to Turkey if no other match
            return "TR";
        }
    }

    // Ticketmaster API Models
    public class TicketmasterResponse
    {
        [JsonPropertyName("_embedded")]
        public TicketmasterEmbedded? Embedded { get; set; }
        public TicketmasterPage? Page { get; set; }
    }

    public class TicketmasterEmbedded
    {
        public List<TicketmasterEvent>? Events { get; set; }
    }

    public class TicketmasterPage
    {
        public int Size { get; set; }
        public int TotalElements { get; set; }
        public int TotalPages { get; set; }
        public int Number { get; set; }
    }

    public class TicketmasterEvent
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Info { get; set; }
        public string? Url { get; set; }
        public List<TicketmasterImage>? Images { get; set; }
        public TicketmasterDates? Dates { get; set; }
        public List<TicketmasterClassification>? Classifications { get; set; }
        public List<TicketmasterPriceRange>? PriceRanges { get; set; }

        [JsonPropertyName("_embedded")]
        public TicketmasterEventEmbedded? Embedded { get; set; }
    }

    public class TicketmasterImage
    {
        public string? Ratio { get; set; }
        public string? Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Fallback { get; set; }
    }

    public class TicketmasterDates
    {
        public TicketmasterStart? Start { get; set; }
        public TicketmasterEnd? End { get; set; }
        public string? Timezone { get; set; }
        public TicketmasterStatus? Status { get; set; }
        public bool SpanMultipleDays { get; set; }
    }

    public class TicketmasterStart
    {
        public string? LocalDate { get; set; }
        public string? LocalTime { get; set; }
        public string? DateTime { get; set; }
    }

    public class TicketmasterEnd
    {
        public string? LocalDate { get; set; }
        public string? LocalTime { get; set; }
        public string? DateTime { get; set; }
    }

    public class TicketmasterStatus
    {
        public string? Code { get; set; }
    }

    public class TicketmasterClassification
    {
        public bool Primary { get; set; }
        public TicketmasterSegment? Segment { get; set; }
        public TicketmasterGenre? Genre { get; set; }
        public TicketmasterSubGenre? SubGenre { get; set; }
    }

    public class TicketmasterSegment
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    public class TicketmasterGenre
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    public class TicketmasterSubGenre
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    public class TicketmasterPriceRange
    {
        public string? Type { get; set; }
        public string? Currency { get; set; }
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
    }

    public class TicketmasterEventEmbedded
    {
        public List<TicketmasterVenue>? Venues { get; set; }
    }

    public class TicketmasterVenue
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Id { get; set; }
        public TicketmasterAddress? Address { get; set; }
        public TicketmasterCity? City { get; set; }
        public TicketmasterCountry? Country { get; set; }
        public TicketmasterLocation? Location { get; set; }
    }

    public class TicketmasterAddress
    {
        public string? Line1 { get; set; }
    }

    public class TicketmasterCity
    {
        public string? Name { get; set; }
    }

    public class TicketmasterCountry
    {
        public string? Name { get; set; }
        public string? CountryCode { get; set; }
    }

    public class TicketmasterLocation
    {
        public string? Longitude { get; set; }
        public string? Latitude { get; set; }
    }
}