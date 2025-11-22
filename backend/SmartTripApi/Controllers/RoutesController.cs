using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTripApi.Data;
using SmartTripApi.DTOs;
using SmartTripApi.Models;
using SmartTripApi.Services.AI;
using System.Security.Claims;
using System.Text.Json;

namespace SmartTripApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoutesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AIService _aiService;
        private readonly ILogger<RoutesController> _logger;

        public RoutesController(AppDbContext context, AIService aiService, ILogger<RoutesController> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        [HttpPost("plan")]
        [Authorize]
        public async Task<ActionResult<RoutePlanResponseDto>> CreateRoutePlan([FromBody] RoutePlanRequestDto request)
        {
            AIRequest? aiRequest = null;
            
            try
            {
                // Get user ID from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                // Validate user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                _logger.LogInformation("Generating route plan for user {UserId} in region {Region}", userId, request.Region);

                // Create AI request log with pending status
                var requestPayloadJson = JsonSerializer.Serialize(new
                {
                    region = request.Region,
                    days = request.Days,
                    theme = request.GetThemeString(),
                    budget = request.GetBudgetString(),
                    intensity = request.GetIntensityString(),
                    transport = request.GetTransportString()
                });

                aiRequest = new AIRequest
                {
                    UserId = userId,
                    RequestPayload = JsonDocument.Parse(requestPayloadJson),
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.AIRequests.Add(aiRequest);
                await _context.SaveChangesAsync();

                // Call AI service to generate route plan
                var aiResponse = await _aiService.GenerateRoutePlanAsync(request);

                // Create itinerary
                var itinerary = new Itinerary
                {
                    UserId = userId,
                    Name = aiResponse.PlanName,
                    Region = request.Region,
                    DaysCount = request.Days,
                    Theme = ConvertToThemeEnum(request.GetThemeString()),
                    Budget = ConvertToBudgetEnum(request.GetBudgetString()),
                    Intensity = ConvertToIntensityEnum(request.GetIntensityString()),
                    Transport = ConvertToTransportEnum(request.GetTransportString()),
                    IsAiGenerated = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Itineraries.Add(itinerary);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created itinerary {ItineraryId} for user {UserId}", itinerary.Id, userId);

                // Process each day and its activities
                var responseDays = new List<DayDetailDto>();

                foreach (var aiDay in aiResponse.Days)
                {
                    // Create itinerary day
                    var itineraryDay = new ItineraryDay
                    {
                        ItineraryId = itinerary.Id,
                        DayNumber = aiDay.DayNumber,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ItineraryDays.Add(itineraryDay);
                    await _context.SaveChangesAsync();

                    var dayActivities = new List<ActivityDetailDto>();

                    foreach (var aiActivity in aiDay.Activities)
                    {
                        // Find or create place
                        Place? place = null;
                        if (aiActivity.Place != null && !string.IsNullOrEmpty(aiActivity.Place.Name))
                        {
                            // Check if place already exists
                            place = await _context.Places
                                .FirstOrDefaultAsync(p => p.Name == aiActivity.Place.Name && p.City == aiActivity.Place.City);

                            if (place == null)
                            {
                                // Create new place
                                place = new Place
                                {
                                    Name = aiActivity.Place.Name,
                                    Description = aiActivity.Place.Description,
                                    City = aiActivity.Place.City ?? request.Region,
                                    Country = aiActivity.Place.Country ?? "Turkey",
                                    Category = ConvertToThemeEnum(request.GetThemeString()),
                                    CreatedAt = DateTime.UtcNow
                                };

                                _context.Places.Add(place);
                                await _context.SaveChangesAsync();
                            }
                        }

                        // Create activity
                        var activity = new Models.Activity
                        {
                            ItineraryDayId = itineraryDay.Id,
                            PlaceId = place?.Id,
                            Title = aiActivity.Title,
                            Description = aiActivity.Description,
                            Reason = aiActivity.Reason,
                            StartTime = ParseTimeString(aiActivity.StartTime),
                            EndTime = ParseTimeString(aiActivity.EndTime),
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Activities.Add(activity);
                        await _context.SaveChangesAsync();

                        // Add to response
                        dayActivities.Add(new ActivityDetailDto
                        {
                            Title = activity.Title,
                            Description = activity.Description ?? "",
                            Reason = activity.Reason ?? "",
                            StartTime = aiActivity.StartTime,
                            EndTime = aiActivity.EndTime,
                            Place = place != null ? new PlaceDetailDto
                            {
                                Name = place.Name,
                                Description = place.Description,
                                City = place.City,
                                Country = place.Country
                            } : null
                        });
                    }

                    responseDays.Add(new DayDetailDto
                    {
                        DayNumber = aiDay.DayNumber,
                        Activities = dayActivities
                    });
                }

                _logger.LogInformation("Successfully created route plan with {DaysCount} days and {ItineraryId}", 
                    responseDays.Count, itinerary.Id);

                // Update AI request with success status and link to itinerary
                aiRequest.ItineraryId = itinerary.Id;
                aiRequest.Status = "completed";
                aiRequest.AiResponse = JsonDocument.Parse(JsonSerializer.Serialize(aiResponse));
                await _context.SaveChangesAsync();

                var response = new RoutePlanResponseDto
                {
                    ItineraryId = itinerary.Id,
                    PlanName = itinerary.Name,
                    Region = itinerary.Region,
                    DaysCount = itinerary.DaysCount,
                    Days = responseDays
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating route plan for region {Region}", request.Region);
                
                // Update AI request status to failed if it was created
                if (aiRequest != null)
                {
                    aiRequest.Status = "failed";
                    aiRequest.AiResponse = JsonDocument.Parse(JsonSerializer.Serialize(new { error = ex.Message }));
                    await _context.SaveChangesAsync();
                }
                
                return StatusCode(500, new { message = "Failed to create route plan", error = ex.Message });
            }
        }

        private TimeSpan? ParseTimeString(string timeString)
        {
            if (string.IsNullOrEmpty(timeString))
                return null;

            if (TimeSpan.TryParse(timeString, out var time))
                return time;

            return null;
        }

        private ThemeTypeEnum? ConvertToThemeEnum(string theme)
        {
            return theme.ToLower() switch
            {
                "nature" => ThemeTypeEnum.nature,
                "sea" => ThemeTypeEnum.sea,
                "history" => ThemeTypeEnum.history,
                "beach" => ThemeTypeEnum.beach,
                "food" => ThemeTypeEnum.food,
                "photospot" => ThemeTypeEnum.photospot,
                _ => null
            };
        }

        private BudgetLevelEnum? ConvertToBudgetEnum(string budget)
        {
            return budget.ToLower() switch
            {
                "low" => BudgetLevelEnum.low,
                "medium" => BudgetLevelEnum.medium,
                "high" => BudgetLevelEnum.high,
                _ => null
            };
        }

        private IntensityLevelEnum? ConvertToIntensityEnum(string intensity)
        {
            return intensity.ToLower() switch
            {
                "relaxed" => IntensityLevelEnum.relaxed,
                "active" => IntensityLevelEnum.active,
                _ => null
            };
        }

        private TransportModeEnum? ConvertToTransportEnum(string transport)
        {
            return transport.ToLower() switch
            {
                "car" => TransportModeEnum.car,
                "walk" => TransportModeEnum.walk,
                "public_transport" => TransportModeEnum.public_transport,
                _ => null
            };
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<RoutePlanResponseDto>> GetRoutePlan(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var itinerary = await _context.Itineraries
                    .Include(i => i.ItineraryDays)
                        .ThenInclude(d => d.Activities)
                            .ThenInclude(a => a.Place)
                    .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

                if (itinerary == null)
                {
                    return NotFound(new { message = "Route plan not found" });
                }

                var response = new RoutePlanResponseDto
                {
                    ItineraryId = itinerary.Id,
                    PlanName = itinerary.Name,
                    Region = itinerary.Region,
                    DaysCount = itinerary.DaysCount,
                    Days = itinerary.ItineraryDays.OrderBy(d => d.DayNumber).Select(day => new DayDetailDto
                    {
                        DayNumber = day.DayNumber,
                        Activities = day.Activities.Select(a => new ActivityDetailDto
                        {
                            Title = a.Title,
                            Description = a.Description ?? "",
                            Reason = a.Reason ?? "",
                            StartTime = a.StartTime?.ToString(@"hh\:mm") ?? "",
                            EndTime = a.EndTime?.ToString(@"hh\:mm") ?? "",
                            Place = a.Place != null ? new PlaceDetailDto
                            {
                                Name = a.Place.Name,
                                Description = a.Place.Description,
                                City = a.Place.City,
                                Country = a.Place.Country
                            } : null
                        }).ToList()
                    }).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving route plan {Id}", id);
                return StatusCode(500, new { message = "Failed to retrieve route plan", error = ex.Message });
            }
        }

        [HttpGet("user")]
        [Authorize]
        public async Task<ActionResult<List<object>>> GetUserRoutePlans()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var itineraries = await _context.Itineraries
                    .Where(i => i.UserId == userId)
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new
                    {
                        i.Id,
                        i.Name,
                        i.Region,
                        i.DaysCount,
                        i.Theme,
                        i.Budget,
                        i.Intensity,
                        i.Transport,
                        i.IsAiGenerated,
                        i.CreatedAt
                    })
                    .ToListAsync();

                return Ok(itineraries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user route plans");
                return StatusCode(500, new { message = "Failed to retrieve route plans", error = ex.Message });
            }
        }
    }
}
