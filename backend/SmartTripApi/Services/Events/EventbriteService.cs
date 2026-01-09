using System.Text.Json;
using System.Text.Json.Serialization;
using SmartTripApi.DTOs;

namespace SmartTripApi.Services.Events
{
    public class EventbriteService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventbriteService> _logger;
        private readonly string _apiToken;
        private readonly string _baseUrl = "https://www.eventbriteapi.com/v3";

        public EventbriteService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<EventbriteService> logger)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _configuration = configuration;
            _logger = logger;
            _apiToken = _configuration["EventbriteApi:Token"] 
                ?? throw new InvalidOperationException("Eventbrite API token not found");
        }

        public async Task<EventSearchResponseDto> SearchEventsAsync(EventSearchRequestDto request)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"token={_apiToken}",
                    $"location.address={Uri.EscapeDataString(request.Location)}",
                    $"location.within=25km",
                    $"expand=venue,format,category,subcategory,bookmark_info,refund_policy,ticket_availability,logo",
                    $"page_size={Math.Min(request.Limit ?? 50, 50)}" // Eventbrite max is 50
                };

                if (request.StartDate != default(DateTime))
                {
                    var startDate = request.StartDate.ToString("yyyy-MM-ddTHH:mm:ss");
                    queryParams.Add($"start_date.range_start={startDate}");
                }

                if (request.EndDate != default(DateTime))
                {
                    var endDate = request.EndDate.ToString("yyyy-MM-ddTHH:mm:ss");
                    queryParams.Add($"start_date.range_end={endDate}");
                }

                if (!string.IsNullOrEmpty(request.EventType))
                {
                    // Map our event types to Eventbrite categories
                    var categoryId = MapEventTypeToEventbriteCategory(request.EventType);
                    if (!string.IsNullOrEmpty(categoryId))
                    {
                        queryParams.Add($"categories={categoryId}");
                    }
                }

                // Add sorting for better results
                queryParams.Add("sort_by=date");

                var url = $"{_baseUrl}/events/search/?{string.Join("&", queryParams)}";
                
                _logger.LogInformation("Searching Eventbrite events with URL: {Url}", url.Replace(_apiToken, "***"));

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("Eventbrite API raw response: {Content}", content.Substring(0, Math.Min(500, content.Length)));
                
                var eventbriteResponse = JsonSerializer.Deserialize<EventbriteResponse>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                _logger.LogInformation("Found {EventCount} events from Eventbrite", 
                    eventbriteResponse?.Events?.Count ?? 0);

                var eventDtos = new List<EventDto>();
                
                if (eventbriteResponse?.Events != null)
                {
                    foreach (var ebEvent in eventbriteResponse.Events)
                    {
                        var eventDto = MapToEventDto(ebEvent);
                        eventDtos.Add(eventDto);
                    }
                }

                _logger.LogInformation("Mapped {EventCount} events from Eventbrite for location: {Location}", 
                    eventDtos.Count, request.Location);

                return new EventSearchResponseDto
                {
                    Events = eventDtos,
                    TotalCount = eventbriteResponse?.Pagination?.ObjectCount ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events from Eventbrite for location: {Location}", request.Location);
                return new EventSearchResponseDto
                {
                    Events = new List<EventDto>(),
                    TotalCount = 0
                };
            }
        }

        private EventDto MapToEventDto(EventbriteEvent ebEvent)
        {
            var eventDto = new EventDto
            {
                Id = ebEvent.Id ?? Guid.NewGuid().ToString(),
                Name = ebEvent.Name?.Text ?? "Unnamed Event",
                Description = ebEvent.Description?.Text,
                Source = "Eventbrite"
            };

            // Dates
            if (ebEvent.Start != null && DateTime.TryParse(ebEvent.Start.Utc, out var startDate))
            {
                eventDto.StartDate = startDate;
            }

            if (ebEvent.End != null && DateTime.TryParse(ebEvent.End.Utc, out var endDate))
            {
                eventDto.EndDate = endDate;
            }

            // Image - Use logo or fallback to category-based placeholder
            if (ebEvent.Logo?.Original?.Url != null)
            {
                eventDto.ImageUrl = ebEvent.Logo.Original.Url;
            }
            else
            {
                var eventType = ebEvent.Category?.Name?.ToLower() ?? "event";
                eventDto.ImageUrl = GetCategoryImageUrl(eventType);
            }

            // Venue
            if (ebEvent.Venue != null)
            {
                eventDto.Venue = new EventVenueDto
                {
                    Name = ebEvent.Venue.Name ?? string.Empty,
                    Address = ebEvent.Venue.Address?.Address1,
                    City = ebEvent.Venue.Address?.City,
                    Country = ebEvent.Venue.Address?.Country
                };

                // Add coordinates if available
                if (ebEvent.Venue.Address != null)
                {
                    if (double.TryParse(ebEvent.Venue.Address.Latitude, out var lat))
                        eventDto.Venue.Latitude = lat;
                    if (double.TryParse(ebEvent.Venue.Address.Longitude, out var lng))
                        eventDto.Venue.Longitude = lng;
                }
            }

            // Event Type/Category
            eventDto.EventType = ebEvent.Category?.Name;
            eventDto.Genre = ebEvent.Subcategory?.Name;

            // Ticket URL
            eventDto.TicketUrl = ebEvent.Url;

            // Price - Eventbrite doesn't always provide this in search results
            if (ebEvent.IsFree == true)
            {
                eventDto.PriceRange = new EventPriceDto
                {
                    Min = 0,
                    Max = 0,
                    Currency = "USD"
                };
            }

            return eventDto;
        }

        private string? MapEventTypeToEventbriteCategory(string eventType)
        {
            return eventType.ToLower() switch
            {
                "music" => "103", // Music
                "sports" => "108", // Sports & Fitness
                "arts" => "105", // Performing & Visual Arts
                "theatre" => "105", // Performing & Visual Arts
                "family" => "115", // Family & Education
                "film" => "104", // Film & Media
                "food" => "110", // Food & Drink
                "business" => "101", // Business & Professional
                "health" => "107", // Health & Wellness
                "community" => "113", // Community & Culture
                "fashion" => "106", // Fashion & Beauty
                "auto" => "118", // Auto, Boat & Air
                "charity" => "111", // Charity & Causes
                "dating" => "112", // Dating
                "education" => "102", // Science & Technology
                "government" => "116", // Government & Politics
                "comedy" => "109", // Comedy
                "spirituality" => "114", // Spirituality & Religion
                "travel" => "119", // Travel & Outdoor
                _ => null // No specific category mapping
            };
        }

        private string GetCategoryImageUrl(string eventType)
        {
            var random = new Random();
            
            return eventType switch
            {
                // Music Events
                "music" or "concerts" => random.Next(3) switch
                {
                    0 => "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80",
                    1 => "https://images.unsplash.com/photo-1459749411175-04bf5292ceea?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80", 
                    _ => "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"
                },
                
                // Sports Events
                "sports" or "fitness" => random.Next(3) switch
                {
                    0 => "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80",
                    1 => "https://images.unsplash.com/photo-1574629810360-7efbbe195018?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80",
                    _ => "https://images.unsplash.com/photo-1540747913346-19e32dc3e97e?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"
                },
                
                // Arts Events
                "arts" or "performing" or "visual" => random.Next(3) switch
                {
                    0 => "https://images.unsplash.com/photo-1507676184212-d03ab07a01bf?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80",
                    1 => "https://images.unsplash.com/photo-1581833971358-2c8b550f87b3?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80",
                    _ => "https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"
                },
                
                // Business Events
                "business" or "professional" or "networking" => random.Next(2) switch
                {
                    0 => "https://images.unsplash.com/photo-1559136555-9303baea8ebd?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80",
                    _ => "https://images.unsplash.com/photo-1515187029135-18ee286d815b?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"
                },
                
                // Food Events
                "food" or "drink" or "culinary" => random.Next(3) switch
                {
                    0 => "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80",
                    1 => "https://images.unsplash.com/photo-1551218808-94e220e084d2?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80",
                    _ => "https://images.unsplash.com/photo-1567620905732-2d1ec7ab7445?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"
                },
                
                // Technology Events
                "technology" or "science" or "tech" => random.Next(2) switch
                {
                    0 => "https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80",
                    _ => "https://images.unsplash.com/photo-1531482615713-2afd69097998?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"
                },
                
                // Default event image
                _ => random.Next(3) switch
                {
                    0 => "https://images.unsplash.com/photo-1501281668745-f7f57925c3b4?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80",
                    1 => "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80",
                    _ => "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=400&h=300&fit=crop&crop=entropy&auto=format&q=80"
                }
            };
        }
    }

    // Eventbrite API Models
    public class EventbriteResponse
    {
        public EventbritePagination? Pagination { get; set; }
        public List<EventbriteEvent>? Events { get; set; }
    }

    public class EventbritePagination
    {
        public int ObjectCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public bool HasMoreItems { get; set; }
    }

    public class EventbriteEvent
    {
        public string? Id { get; set; }
        public EventbriteText? Name { get; set; }
        public EventbriteText? Description { get; set; }
        public string? Url { get; set; }
        public EventbriteDateTime? Start { get; set; }
        public EventbriteDateTime? End { get; set; }
        public EventbriteLogo? Logo { get; set; }
        public EventbriteVenue? Venue { get; set; }
        public EventbriteCategory? Category { get; set; }
        public EventbriteCategory? Subcategory { get; set; }
        public bool? IsFree { get; set; }
        public string? Status { get; set; }
    }

    public class EventbriteText
    {
        public string? Text { get; set; }
        public string? Html { get; set; }
    }

    public class EventbriteDateTime
    {
        public string? Timezone { get; set; }
        public string? Local { get; set; }
        public string? Utc { get; set; }
    }

    public class EventbriteLogo
    {
        public EventbriteImage? Original { get; set; }
    }

    public class EventbriteImage
    {
        public string? Url { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }

    public class EventbriteVenue
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public EventbriteAddress? Address { get; set; }
    }

    public class EventbriteAddress
    {
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
    }

    public class EventbriteCategory
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? ShortName { get; set; }
    }
}
