using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTripApi.DTOs;
using SmartTripApi.Services.Events;

namespace SmartTripApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ILogger<EventsController> _logger;

        public EventsController(IEventService eventService, ILogger<EventsController> logger)
        {
            _eventService = eventService;
            _logger = logger;
        }

        [HttpPost("search")]
        [Authorize]
        public async Task<ActionResult<EventSearchResponseDto>> SearchEvents([FromBody] EventSearchRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Tarih validasyonu
                if (request.StartDate >= request.EndDate)
                {
                    return BadRequest(new { message = "Start date must be before end date" });
                }

                if (request.StartDate < DateTime.Now.Date)
                {
                    return BadRequest(new { message = "Start date cannot be in the past" });
                }

                // Maksimum arama süresi kontrolü (örn. 1 yıl)
                var maxSearchPeriod = TimeSpan.FromDays(365);
                if (request.EndDate - request.StartDate > maxSearchPeriod)
                {
                    return BadRequest(new { message = "Search period cannot exceed 1 year" });
                }

                _logger.LogInformation("Searching events for location: {Location}, dates: {StartDate} - {EndDate}", 
                    request.Location, request.StartDate, request.EndDate);

                var result = await _eventService.SearchEventsAsync(request);
                
                _logger.LogInformation("Found {EventCount} events for location: {Location}", 
                    result.Events.Count, request.Location);

                // Log image URLs for debugging
                foreach (var eventItem in result.Events.Take(3)) // Log first 3 events
                {
                    _logger.LogInformation("Event: {Name}, ImageUrl: {ImageUrl}", eventItem.Name, eventItem.ImageUrl);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events for location: {Location}", request.Location);
                return StatusCode(500, new { message = "Failed to search events", error = ex.Message });
            }
        }

        [HttpGet("types")]
        public IActionResult GetEventTypes()
        {
            var eventTypes = new[]
            {
                new { id = "music", name = "Music", description = "Concerts, festivals, live music" },
                new { id = "sports", name = "Sports", description = "Football, basketball, tennis, etc." },
                new { id = "arts", name = "Arts & Theatre", description = "Plays, musicals, dance performances" },
                new { id = "family", name = "Family", description = "Family-friendly events and shows" },
                new { id = "film", name = "Film", description = "Movie premieres, film festivals" },
                new { id = "miscellaneous", name = "Miscellaneous", description = "Other events and activities" }
            };

            return Ok(eventTypes);
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "Events API is working", timestamp = DateTime.Now });
        }
    }
}
