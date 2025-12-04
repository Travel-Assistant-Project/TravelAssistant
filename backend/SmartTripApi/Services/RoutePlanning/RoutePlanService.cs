using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartTripApi.Data;
using SmartTripApi.DTOs;
using SmartTripApi.Models;
using SmartTripApi.Services.AI;
using SmartTripApi.Services.GooglePlaces;
using SmartTripApi.Services.Weather;
using SmartTripApi.Mappers;
using SmartTripApi.Helpers;

namespace SmartTripApi.Services.RoutePlanning
{
    public class RoutePlanService : IRoutePlanService
    {
        private readonly AppDbContext _context;
        private readonly AIService _aiService;
        private readonly PlaceEnrichmentService _placeEnrichmentService;
        private readonly IWeatherService _weatherService;
        private readonly ILogger<RoutePlanService> _logger;

        public RoutePlanService(
            AppDbContext context,
            AIService aiService,
            PlaceEnrichmentService placeEnrichmentService,
            IWeatherService weatherService,
            ILogger<RoutePlanService> logger)
        {
            _context = context;
            _aiService = aiService;
            _placeEnrichmentService = placeEnrichmentService;
            _weatherService = weatherService;
            _logger = logger;
        }

        public async Task<ServiceResult<RoutePlanResponseDto>> CreateRoutePlanAsync(
            int userId,
            RoutePlanRequestDto request)
        {
            AIRequest? aiRequest = null;

            try
            {
                // user var mı
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return ServiceResult<RoutePlanResponseDto>.Fail(404, "User not found");
                }

                _logger.LogInformation("Generating route plan for user {UserId} in region {Region}",
                    userId, request.Region);

                // AI request log
                var requestPayloadJson = JsonSerializer.Serialize(new
                {
                    region = request.Region,
                    days = request.Days,
                    themes = request.GetThemeStrings(),
                    budgets = request.GetBudgetStrings(),
                    intensities = request.GetIntensityStrings(),
                    transports = request.GetTransportStrings()
                });

                aiRequest = new AIRequest
                {
                    UserId = userId,
                    RequestPayload = JsonDocument.Parse(requestPayloadJson),
                    Status = "pending",
                    CreatedAt = DateTimeHelper.GetTurkeyTime()
                };

                _context.AIRequests.Add(aiRequest);
                await _context.SaveChangesAsync();

                // AI çağrısı
                var aiResponse = await _aiService.GenerateRoutePlanAsync(request);

                // primary seçimler
                var primaryTheme = request.GetPrimaryTheme();
                var primaryBudget = request.GetPrimaryBudget();
                var primaryIntensity = request.GetPrimaryIntensity();
                var primaryTransport = request.GetPrimaryTransport();

                // Itinerary oluştur
                var itinerary = new Itinerary
                {
                    UserId = userId,
                    Name = aiResponse.PlanName,
                    Region = request.Region,
                    DaysCount = request.Days,
                    Theme = primaryTheme.HasValue
                        ? ConvertToThemeEnum(primaryTheme.Value.ToString().ToLower())
                        : null,
                    Budget = primaryBudget.HasValue
                        ? ConvertToBudgetEnum(primaryBudget.Value.ToString().ToLower())
                        : null,
                    Intensity = primaryIntensity.HasValue
                        ? ConvertToIntensityEnum(primaryIntensity.Value.ToString().ToLower())
                        : null,
                    Transport = primaryTransport.HasValue
                        ? ConvertToTransportEnum(primaryTransport.Value.ToString().ToLower())
                        : null,
                    IsAiGenerated = true,
                    Status = "pending",
                    CreatedAt = DateTimeHelper.GetTurkeyTime()
                };

                _context.Itineraries.Add(itinerary);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created itinerary {ItineraryId} for user {UserId}",
                    itinerary.Id, userId);

                var responseDays = new List<DayDetailDto>();

                // Gün + aktiviteler
                foreach (var aiDay in aiResponse.Days)
                {
                    var itineraryDay = new ItineraryDay
                    {
                        ItineraryId = itinerary.Id,
                        DayNumber = aiDay.DayNumber,
                        CreatedAt = DateTimeHelper.GetTurkeyTime()
                    };

                    _context.ItineraryDays.Add(itineraryDay);
                    await _context.SaveChangesAsync();

                    var dayActivities = new List<ActivityDetailDto>();

                    foreach (var aiActivity in aiDay.Activities)
                    {
                        try
                        {
                            // Place bul / oluştur
                            Place? place = null;
                            if (aiActivity.Place != null && !string.IsNullOrEmpty(aiActivity.Place.Name))
                            {
                                place = await _context.Places
                                    .FirstOrDefaultAsync(p =>
                                        p.Name == aiActivity.Place.Name &&
                                        p.City == aiActivity.Place.City);

                                if (place == null)
                                {
                                    place = new Place
                                    {
                                        Name = aiActivity.Place.Name,
                                        Description = aiActivity.Place.Description,
                                        City = aiActivity.Place.City ?? request.Region,
                                        Country = aiActivity.Place.Country ?? "Turkey",
                                        Category = primaryTheme.HasValue
                                            ? ConvertToThemeEnum(primaryTheme.Value.ToString().ToLower())
                                            : null,
                                        CreatedAt = DateTimeHelper.GetTurkeyTime()
                                    };

                                    _context.Places.Add(place);
                                    await _context.SaveChangesAsync();
                                }
                            }

                            // Parse time strings safely
                            var startTime = ParseTimeString(aiActivity.StartTime ?? "");
                            var endTime = ParseTimeString(aiActivity.EndTime ?? "");

                            // Activity
                            var activity = new Models.Activity
                            {
                                ItineraryDayId = itineraryDay.Id,
                                PlaceId = place?.Id,
                                Title = aiActivity.Title ?? "Untitled Activity",
                                Description = aiActivity.Description ?? "",
                                Reason = aiActivity.Reason ?? "",
                                StartTime = startTime,
                                EndTime = endTime,
                                CreatedAt = DateTimeHelper.GetTurkeyTime()
                            };

                            _context.Activities.Add(activity);
                            await _context.SaveChangesAsync();

                            dayActivities.Add(new ActivityDetailDto
                            {
                                Title = activity.Title,
                                Description = activity.Description ?? "",
                                Reason = activity.Reason ?? "",
                                StartTime = aiActivity.StartTime ?? "",
                                EndTime = aiActivity.EndTime ?? "",
                                Place = place != null
                                    ? new PlaceDetailDto
                                    {
                                        Name = place.Name,
                                        Description = place.Description,
                                        City = place.City,
                                        Country = place.Country,
                                        ImageUrls = place.PhotoUrls != null && place.PhotoUrls.Length > 0
                                            ? place.PhotoUrls.ToList()
                                            : null,
                                        GoogleRating = place.GoogleRating,
                                        Latitude = place.Latitude,
                                        Longitude = place.Longitude
                                    }
                                    : null
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, 
                                "Error processing activity '{ActivityTitle}' for day {DayNumber}. Skipping this activity.", 
                                aiActivity.Title ?? "Unknown", aiDay.DayNumber);
                            // Continue with next activity instead of failing entire request
                        }
                    }

                    responseDays.Add(new DayDetailDto
                    {
                        DayNumber = aiDay.DayNumber,
                        Activities = dayActivities
                    });
                }

                _logger.LogInformation(
                    "Successfully created route plan with {DaysCount} days and {ItineraryId}",
                    responseDays.Count, itinerary.Id);

                // AIRequest update
                aiRequest.ItineraryId = itinerary.Id;
                aiRequest.Status = "completed";
                aiRequest.AiResponse = JsonDocument.Parse(JsonSerializer.Serialize(aiResponse));
                await _context.SaveChangesAsync();

                // Enrichment
                try
                {
                    _logger.LogInformation("Starting enrichment (places + weather) for itinerary {ItineraryId}",
                        itinerary.Id);

                    itinerary.Status = "processing";
                    await _context.SaveChangesAsync();

                    await _placeEnrichmentService.EnrichItineraryPlacesAsync(itinerary.Id);
                    await _weatherService.UpdateItineraryWeatherAsync(itinerary.Id);

                    itinerary.Status = "completed";
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Completed enrichment (places + weather) for itinerary {ItineraryId}",
                        itinerary.Id);
                }
                catch (Exception enrichmentEx)
                {
                    _logger.LogError(enrichmentEx,
                        "Error enriching places/weather for itinerary {ItineraryId}", itinerary.Id);

                    itinerary.Status = "completed_with_warnings";
                    await _context.SaveChangesAsync();
                }

                var response = new RoutePlanResponseDto
                {
                    ItineraryId = itinerary.Id,
                    PlanName = itinerary.Name,
                    Region = itinerary.Region,
                    DaysCount = itinerary.DaysCount,
                    Days = responseDays
                };

                return ServiceResult<RoutePlanResponseDto>.Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating route plan for region {Region}", request.Region);

                if (aiRequest != null)
                {
                    aiRequest.Status = "failed";
                    aiRequest.AiResponse = JsonDocument.Parse(
                        JsonSerializer.Serialize(new { error = ex.Message }));
                    await _context.SaveChangesAsync();
                }

                return ServiceResult<RoutePlanResponseDto>.Fail(500, "Failed to create route plan");
            }
        }

        public async Task<RoutePlanResponseDto?> GetRoutePlanAsync(int userId, int itineraryId)
        {
            var itinerary = await _context.Itineraries
                .Include(i => i.ItineraryDays)
                    .ThenInclude(d => d.Activities)
                        .ThenInclude(a => a.Place)
                .FirstOrDefaultAsync(i => i.Id == itineraryId && i.UserId == userId);

            if (itinerary == null)
                return null;

            return RoutePlanMapper.ToRoutePlanResponseDto(itinerary);
        }

        public async Task<List<UserRouteSummaryDto>> GetUserRoutePlansAsync(int userId)
        {
            return await _context.Itineraries
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new UserRouteSummaryDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Region = i.Region,
                    DaysCount = i.DaysCount,
                    Theme = i.Theme,
                    Budget = i.Budget,
                    Intensity = i.Intensity,
                    Transport = i.Transport,
                    IsAiGenerated = i.IsAiGenerated,
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();
        }

        // ---- helperlar ----

        private TimeSpan? ParseTimeString(string timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
                return null;

            // Temizle - whitespace ve özel karakterleri kaldır
            timeString = timeString.Trim()
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\n", "")
                .Replace("\r", "");

            if (string.IsNullOrEmpty(timeString))
                return null;

            try
            {
                // Önce standart TimeSpan.TryParse dene (InvariantCulture ile)
                if (TimeSpan.TryParse(timeString, System.Globalization.CultureInfo.InvariantCulture, out var time))
                    return time;

                // "HH:mm" formatında deneme (örn: "09:00", "14:30")
                var formats = new[] { "HH:mm", "H:mm", "hh:mm", "h:mm", "HH:mm:ss", "H:mm:ss", "h:mm tt", "hh:mm tt" };
                
                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(timeString, format, 
                        System.Globalization.CultureInfo.InvariantCulture, 
                        System.Globalization.DateTimeStyles.None, 
                        out var dateTime))
                    {
                        return dateTime.TimeOfDay;
                    }
                }

                // Manuel parse denemesi (örn: "9:00" -> 9 saat, 0 dakika)
                var parts = timeString.Split(':');
                if (parts.Length >= 2 && 
                    int.TryParse(parts[0], out var hours) && 
                    int.TryParse(parts[1], out var minutes))
                {
                    if (hours >= 0 && hours < 24 && minutes >= 0 && minutes < 60)
                    {
                        return new TimeSpan(hours, minutes, 0);
                    }
                }

                _logger.LogWarning("Could not parse time string: {TimeString}", timeString);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing time string: {TimeString}", timeString);
                return null;
            }
        }

        private ThemeTypeEnum? ConvertToThemeEnum(string theme) =>
            theme.ToLower() switch
            {
                "nature" => ThemeTypeEnum.nature,
                "sea" => ThemeTypeEnum.sea,
                "history" => ThemeTypeEnum.history,
                "beach" => ThemeTypeEnum.beach,
                "food" => ThemeTypeEnum.food,
                "photospot" => ThemeTypeEnum.photospot,
                _ => null
            };

        private BudgetLevelEnum? ConvertToBudgetEnum(string budget) =>
            budget.ToLower() switch
            {
                "low" => BudgetLevelEnum.low,
                "medium" => BudgetLevelEnum.medium,
                "high" => BudgetLevelEnum.high,
                _ => null
            };

        private IntensityLevelEnum? ConvertToIntensityEnum(string intensity) =>
            intensity.ToLower() switch
            {
                "relaxed" => IntensityLevelEnum.relaxed,
                "active" => IntensityLevelEnum.active,
                _ => null
            };

        private TransportModeEnum? ConvertToTransportEnum(string transport) =>
            transport.ToLower() switch
            {
                "car" => TransportModeEnum.car,
                "walk" => TransportModeEnum.walk,
                "public_transport" => TransportModeEnum.public_transport,
                _ => null
            };
    }
}
