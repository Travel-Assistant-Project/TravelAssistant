using SmartTripApi.DTOs;

namespace SmartTripApi.Services.Events
{
    public class HybridEventService : IEventService
    {
        private readonly TicketmasterService _ticketmasterService;
        private readonly EventbriteService _eventbriteService;
        private readonly ILogger<HybridEventService> _logger;

        public HybridEventService(
            TicketmasterService ticketmasterService,
            EventbriteService eventbriteService,
            ILogger<HybridEventService> logger)
        {
            _ticketmasterService = ticketmasterService;
            _eventbriteService = eventbriteService;
            _logger = logger;
        }

        public async Task<EventSearchResponseDto> SearchEventsAsync(EventSearchRequestDto request)
        {
            _logger.LogInformation("Starting hybrid event search for location: {Location}", request.Location);

            var allEvents = new List<EventDto>();
            var totalCount = 0;

            // First, try Ticketmaster
            try
            {
                _logger.LogInformation("Trying Ticketmaster API for location: {Location}", request.Location);
                var ticketmasterResponse = await _ticketmasterService.SearchEventsAsync(request);
                
                if (ticketmasterResponse.Events.Any())
                {
                    _logger.LogInformation("Found {Count} events from Ticketmaster", ticketmasterResponse.Events.Count);
                    allEvents.AddRange(ticketmasterResponse.Events);
                    totalCount += ticketmasterResponse.TotalCount;
                }
                else
                {
                    _logger.LogInformation("No events found from Ticketmaster for location: {Location}", request.Location);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching from Ticketmaster for location: {Location}", request.Location);
            }

            // Then, try Eventbrite (always try for more comprehensive results)
            try
            {
                _logger.LogInformation("Trying Eventbrite API for location: {Location}", request.Location);
                var eventbriteResponse = await _eventbriteService.SearchEventsAsync(request);
                
                if (eventbriteResponse.Events.Any())
                {
                    _logger.LogInformation("Found {Count} events from Eventbrite", eventbriteResponse.Events.Count);
                    
                    // Add Eventbrite events, avoiding duplicates based on name and date
                    foreach (var ebEvent in eventbriteResponse.Events)
                    {
                        var isDuplicate = allEvents.Any(existing => 
                            IsSimilarEvent(existing, ebEvent));
                        
                        if (!isDuplicate)
                        {
                            allEvents.Add(ebEvent);
                        }
                    }
                    totalCount += eventbriteResponse.TotalCount;
                }
                else
                {
                    _logger.LogInformation("No events found from Eventbrite for location: {Location}", request.Location);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching from Eventbrite for location: {Location}", request.Location);
            }

            // Sort events by date
            allEvents = allEvents
                .OrderBy(e => e.StartDate)
                .Take(request.Limit ?? 50)
                .ToList();

            _logger.LogInformation("Hybrid search completed. Total unique events: {Count} for location: {Location}", 
                allEvents.Count, request.Location);

            // If we still have no events, provide some helpful information
            if (!allEvents.Any())
            {
                _logger.LogWarning("No events found from any source for location: {Location}. " +
                                 "This could be due to API limits, network issues, or no events in the area.", 
                                 request.Location);
            }

            return new EventSearchResponseDto
            {
                Events = allEvents,
                TotalCount = totalCount,
                SearchLocation = request.Location,
                SearchStartDate = request.StartDate,
                SearchEndDate = request.EndDate
            };
        }

        private bool IsSimilarEvent(EventDto event1, EventDto event2)
        {
            // Check if events are similar based on name and date
            if (string.IsNullOrEmpty(event1.Name) || string.IsNullOrEmpty(event2.Name))
                return false;

            // Similar name check (case insensitive, remove common words)
            var name1 = NormalizeName(event1.Name);
            var name2 = NormalizeName(event2.Name);
            
            var nameSimilarity = CalculateStringSimilarity(name1, name2);
            
            // Similar date check (same day)
            var sameDateRange = Math.Abs((event1.StartDate - event2.StartDate).TotalHours) < 24;
            
            // Consider events similar if names are >80% similar and dates are within 24 hours
            return nameSimilarity > 0.8 && sameDateRange;
        }

        private string NormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            
            // Remove common words and normalize
            var commonWords = new[] { "the", "and", "or", "at", "in", "on", "with", "by", "for", "of", "to" };
            var words = name.ToLower()
                           .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                           .Where(word => !commonWords.Contains(word))
                           .ToArray();
            
            return string.Join(" ", words);
        }

        private double CalculateStringSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0;
            if (s1 == s2) return 1.0;

            // Simple Levenshtein distance-based similarity
            var distance = LevenshteinDistance(s1, s2);
            var maxLength = Math.Max(s1.Length, s2.Length);
            
            return maxLength == 0 ? 1.0 : 1.0 - (double)distance / maxLength;
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            var matrix = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                matrix[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[s1.Length, s2.Length];
        }
    }
}
