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
            eventDto.ImageUrl = GetCategoryImageUrl(eventType);
            
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

        private string GetCategoryImageUrl(string eventType)
        {
            var random = new Random();
            
            return eventType switch
            {
                // Music Events - Concert and music specific images
                "music" or "concerts" => random.Next(4) switch
                {
                    0 => "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // concert crowd
                    1 => "https://images.unsplash.com/photo-1459749411175-04bf5292ceea?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // concert stage
                    _ => "https://images.unsplash.com/photo-1506157786151-b8491531f063?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"  // live music
                },
                
                // Sports Events - Different sports images
                "sports" => random.Next(4) switch
                {
                    0 => "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // stadium
                    1 => "https://images.unsplash.com/photo-1574629810360-7efbbe195018?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // football
                    2 => "https://images.unsplash.com/photo-1546519638-68e109498ffc?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // basketball
                    _ => "https://images.unsplash.com/photo-1540747913346-19e32dc3e97e?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"  // tennis
                },
                
                // Arts & Theatre - Performance and stage images
                "arts" or "theatre" or "theater" => random.Next(1) switch
                {
                    0 => "https://images.unsplash.com/photo-1507676184212-d03ab07a01bf?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // theatre curtain
                },
                
                // Family Events - Family-friendly activities
                "family" => random.Next(3) switch
                {
                    0 => "https://images.unsplash.com/photo-1511593358241-7eea1f3c84e5?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // carnival
                    1 => "https://images.unsplash.com/photo-1533924721034-8b3da3f2b735?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // amusement park
                    _ => "https://images.unsplash.com/photo-1530103862676-de8c9debad1d?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"  // family fun
                },
                
                // Film Events - Cinema and movie related
                "film" => random.Next(3) switch
                {
                    0 => "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // cinema
                    1 => "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // film reel
                    _ => "https://images.unsplash.com/photo-1440404653325-ab127d49abc1?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"  // movie theater
                },
                
                // Comedy Events - Comedy and entertainment
                "comedy" => random.Next(2) switch
                {
                    0 => "https://images.unsplash.com/photo-1516450360452-9312f5e86fc7?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // comedy club
                    _ => "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"  // microphone on stage
                },
                
                // Dance Events - Dance performances
                "dance" => random.Next(3) switch
                {
                    0 => "https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // ballet
                    1 => "https://images.unsplash.com/photo-1547036967-23d11aacaee0?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // modern dance
                    _ => "https://images.unsplash.com/photo-1508700929628-666bc8bd84ea?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"  // dance floor
                },
                
                // Festival Events - Festival atmosphere
                "festival" or "fair" => random.Next(3) switch
                {
                    0 => "https://images.unsplash.com/photo-1533174072545-7a4b6ad7a6c3?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // festival crowd
                    1 => "https://images.unsplash.com/photo-1459749411175-04bf5292ceea?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // outdoor festival
                    _ => "https://images.unsplash.com/photo-1549451371-64aa98a6f0b2?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"  // music festival
                },
                
                // Business & Conference Events
                "business" or "conference" => random.Next(2) switch
                {
                    0 => "https://images.unsplash.com/photo-1559136555-9303baea8ebd?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // conference
                    _ => "https://images.unsplash.com/photo-1515187029135-18ee286d815b?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"  // business meeting
                },
                
                // Food & Culinary Events
                "food" or "culinary" => random.Next(3) switch
                {
                    0 => "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // food market
                    1 => "https://images.unsplash.com/photo-1551218808-94e220e084d2?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // cooking
                    _ => "https://images.unsplash.com/photo-1567620905732-2d1ec7ab7445?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"  // restaurant
                },
                
                // Technology & Innovation Events
                "technology" or "tech" or "innovation" => random.Next(2) switch
                {
                    0 => "https://images.unsplash.com/photo-1531482615713-2afd69097998?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // tech conference
                    _ => "https://images.unsplash.com/photo-1505373877841-8d25f7d46678?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"  // technology
                },
                
                // Networking & Social Events
                "networking" or "social" => random.Next(2) switch
                {
                    0 => "https://images.unsplash.com/photo-1511578314322-379afb476865?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // networking
                    _ => "https://images.unsplash.com/photo-1528605105345-5344ea20e269?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"  // social gathering
                },
                
                // Default event image - Generic event venues
                _ => random.Next(4) switch
                {
                    0 => "https://images.unsplash.com/photo-1501281668745-f7f57925c3b4?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // event hall
                    1 => "https://images.unsplash.com/photo-1564484981795-4f34435fdd52?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // event space
                    2 => "https://images.unsplash.com/photo-1517457373958-b7bdd4587205?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", // conference room
                    _ => "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"  // event venue
                }
            };
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