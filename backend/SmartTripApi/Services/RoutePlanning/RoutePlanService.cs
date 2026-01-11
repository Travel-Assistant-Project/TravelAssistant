using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartTripApi.Data;
using SmartTripApi.DTOs;
using SmartTripApi.Helpers;
using SmartTripApi.Mappers;
using SmartTripApi.Models;
using SmartTripApi.Services.AI;
using SmartTripApi.Services.GooglePlaces;
using SmartTripApi.Services.RoutePlanning.Caching;
using SmartTripApi.Services.Weather;

namespace SmartTripApi.Services.RoutePlanning
{
    public class RoutePlanService : IRoutePlanService
    {
        private readonly AppDbContext _context;
        private readonly AIService _aiService;
        private readonly PlaceEnrichmentService _placeEnrichmentService;
        private readonly TransportEnrichmentService _transportEnrichmentService;
        private readonly IWeatherService _weatherService;
        private readonly ILogger<RoutePlanService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        // cache
        private readonly ICacheKeyBuilder _cacheKeyBuilder;
        private readonly IRouteCacheService _routeCache;

        public RoutePlanService(
            AppDbContext context,
            AIService aiService,
            PlaceEnrichmentService placeEnrichmentService,
            TransportEnrichmentService transportEnrichmentService,
            IWeatherService weatherService,
            ILogger<RoutePlanService> logger,
            IServiceScopeFactory scopeFactory,
            ICacheKeyBuilder cacheKeyBuilder,
            IRouteCacheService routeCache)
        {
            _context = context;
            _aiService = aiService;
            _placeEnrichmentService = placeEnrichmentService;
            _transportEnrichmentService = transportEnrichmentService;
            _weatherService = weatherService;
            _logger = logger;
            _scopeFactory = scopeFactory;

            _cacheKeyBuilder = cacheKeyBuilder;
            _routeCache = routeCache;
        }

        public async Task<ServiceResult<RoutePlanResponseDto>> CreateRoutePlanAsync(
            int userId,
            RoutePlanRequestDto request,
            bool forceRegenerate = false)
        {
            var swTotal = Stopwatch.StartNew();
            AIRequest? aiRequest = null;

            string? cacheKey = null;
            string? requestHash = null;
            string? lockToken = null;

            var cacheStatus = forceRegenerate ? "BYPASS" : "MISS";

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    swTotal.Stop();
                    return ServiceResult<RoutePlanResponseDto>.Fail(404, "User not found");
                }

                _logger.LogInformation("Generating route plan for user {UserId} in region {Region}",
                    userId, request.Region);

                //build cache key/hash
                var built = _cacheKeyBuilder.Build(userId, request);
                cacheKey = built.cacheKey;
                requestHash = built.requestHash;

                var ttl = TimeSpan.FromDays(7);

                // -----------------------------
                //  CACHE / FALLBACK / LOCK
                // -----------------------------
                if (!forceRegenerate)
                {

                    var cachedItineraryId = await _routeCache.GetItineraryIdAsync(cacheKey);
                    if (cachedItineraryId.HasValue)
                    {
                        var cachedRoute = await GetRoutePlanAsync(userId, cachedItineraryId.Value);
                        if (cachedRoute != null)
                        {
                            swTotal.Stop();
                            return ServiceResult<RoutePlanResponseDto>.Ok(cachedRoute, "HIT");
                        }
                    }

                    var existingIndex = await _context.ItineraryRequestIndexes
                        .AsNoTracking()
                        .Where(x => x.UserId == userId && x.RequestHash == requestHash)
                        .OrderByDescending(x => x.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (existingIndex != null)
                    {
                        await _routeCache.SetItineraryIdAsync(cacheKey, existingIndex.ItineraryId, ttl);

                        var dbRoute = await GetRoutePlanAsync(userId, existingIndex.ItineraryId);
                        if (dbRoute != null)
                        {
                            swTotal.Stop();
                            return ServiceResult<RoutePlanResponseDto>.Ok(dbRoute, "DB_FALLBACK");
                        }
                    }

                    lockToken = await _routeCache.TryAcquireLockAsync(cacheKey, TimeSpan.FromSeconds(60));
                    if (lockToken == null)
                    {
                        var waitedId = await _routeCache.WaitForItineraryIdAsync(
                            cacheKey,
                            timeout: TimeSpan.FromSeconds(25),
                            pollInterval: TimeSpan.FromMilliseconds(250));

                        if (waitedId.HasValue)
                        {
                            var waitedRoute = await GetRoutePlanAsync(userId, waitedId.Value);
                            if (waitedRoute != null)
                            {
                                swTotal.Stop();
                                return ServiceResult<RoutePlanResponseDto>.Ok(waitedRoute, "WAITED");
                            }
                        }

                        lockToken = await _routeCache.TryAcquireLockAsync(cacheKey, TimeSpan.FromSeconds(60));
                        if (lockToken == null)
                        {
                            cacheStatus = "MISS_LOCKED";
                        }
                        else
                        {
                            cacheStatus = "MISS";
                        }
                    }
                    else
                    {
                        cacheStatus = "MISS";
                    }
                }
                else
                {
                    cacheStatus = "BYPASS";
                }

                // -----------------------------
                // AI request log
                // -----------------------------
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

                // ⏱ AI çağrısı süresi
                var swAi = Stopwatch.StartNew();
                _logger.LogInformation("⏱ AI route plan request started for user {UserId}", userId);

                var aiResponse = await _aiService.GenerateRoutePlanAsync(request);

                swAi.Stop();
                _logger.LogInformation("⏱ AI route plan response completed in {Duration} ms for user {UserId}",
                    swAi.ElapsedMilliseconds, userId);

                // primary seçimler
                var primaryTheme = request.GetPrimaryTheme();
                var primaryBudget = request.GetPrimaryBudget();
                var primaryIntensity = request.GetPrimaryIntensity();
                var primaryTransport = request.GetPrimaryTransport();

                // Itinerary
                var itinerary = new Itinerary
                {
                    UserId = userId,
                    Name = aiResponse.PlanName,
                    Region = request.Region,
                    DaysCount = request.Days,
                    Theme = primaryTheme.HasValue ? ConvertToThemeEnum(primaryTheme.Value.ToString().ToLower()) : null,
                    Budget = primaryBudget.HasValue ? ConvertToBudgetEnum(primaryBudget.Value.ToString().ToLower()) : null,
                    Intensity = primaryIntensity.HasValue ? ConvertToIntensityEnum(primaryIntensity.Value.ToString().ToLower()) : null,
                    Transport = primaryTransport.HasValue ? ConvertToTransportEnum(primaryTransport.Value.ToString().ToLower()) : null,
                    IsAiGenerated = true,
                    Status = "pending",
                    CreatedAt = DateTimeHelper.GetTurkeyTime()
                };

                _context.Itineraries.Add(itinerary);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created itinerary {ItineraryId} for user {UserId}",
                    itinerary.Id, userId);

                if (!string.IsNullOrWhiteSpace(requestHash))
                {
                    var now = DateTimeHelper.GetTurkeyTime();

                    var existing = await _context.ItineraryRequestIndexes
                        .FirstOrDefaultAsync(x => x.UserId == userId && x.RequestHash == requestHash);

                    if (existing == null)
                    {
                        _context.ItineraryRequestIndexes.Add(new ItineraryRequestIndex
                        {
                            UserId = userId,
                            ItineraryId = itinerary.Id,
                            RequestHash = requestHash,
                            CreatedAt = now
                        });
                    }
                    else
                    {
                        // aynı hash varsa: yeni itinerary'yi en güncel yap
                        existing.ItineraryId = itinerary.Id;
                        existing.CreatedAt = now;
                    }

                    await _context.SaveChangesAsync();
                }

                if (!string.IsNullOrWhiteSpace(cacheKey))
                {
                    await _routeCache.SetItineraryIdAsync(cacheKey, itinerary.Id, TimeSpan.FromDays(7));
                }

                var responseDays = new List<DayDetailDto>();

                var swBuild = Stopwatch.StartNew();
                _logger.LogInformation("⏱ Building itinerary days & activities for itinerary {ItineraryId}", itinerary.Id);

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

                            var startTime = ParseTimeString(aiActivity.StartTime ?? "");
                            var endTime = ParseTimeString(aiActivity.EndTime ?? "");

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
                        }
                    }

                    responseDays.Add(new DayDetailDto
                    {
                        DayNumber = aiDay.DayNumber,
                        Activities = dayActivities
                    });
                }

                swBuild.Stop();
                _logger.LogInformation("⏱ Built itinerary days & activities in {Duration} ms for itinerary {ItineraryId}",
                    swBuild.ElapsedMilliseconds, itinerary.Id);

                _logger.LogInformation(
                    "Successfully created route plan with {DaysCount} days and {ItineraryId}",
                    responseDays.Count, itinerary.Id);

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

                // SYNC enrichment - execute synchronously to ensure transport data is available
                try
                {
                    _logger.LogInformation("Starting enrichment for itinerary {ItineraryId}", itinerary.Id);

                    itinerary.Status = "processing";
                    await _context.SaveChangesAsync();

                    await _placeEnrichmentService.EnrichItineraryPlacesAsync(itinerary.Id);
                    await _weatherService.UpdateItineraryWeatherAsync(itinerary.Id);
                    await _transportEnrichmentService.EnrichItineraryTransportAsync(itinerary.Id);

                    itinerary.Status = "completed";
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Completed enrichment for itinerary {ItineraryId}", itinerary.Id);

                    // Fetch the enriched data to return
                    var enrichedRoute = await GetRoutePlanAsync(userId, itinerary.Id);
                    if (enrichedRoute != null)
                    {
                        response = enrichedRoute;
                    }
                }
                catch (Exception enrichmentEx)
                {
                    _logger.LogError(enrichmentEx,
                        "Error during enrichment for itinerary {ItineraryId}", itinerary.Id);
                    
                    itinerary.Status = "completed_with_warnings";
                    await _context.SaveChangesAsync();
                }

                swTotal.Stop();
                _logger.LogInformation("⏱ CreateRoutePlanAsync TOTAL duration: {Duration} ms for user {UserId}",
                    swTotal.ElapsedMilliseconds, userId);

                if (!forceRegenerate && !string.IsNullOrWhiteSpace(lockToken) && !string.IsNullOrWhiteSpace(cacheKey))
                {
                    await _routeCache.ReleaseLockAsync(cacheKey, lockToken);
                }

                return ServiceResult<RoutePlanResponseDto>.Ok(response, cacheStatus);
            }
            catch (Exception ex)
            {
                swTotal.Stop();
                _logger.LogError(ex, "Error creating route plan for region {Region}", request.Region);

                if (!forceRegenerate && !string.IsNullOrWhiteSpace(lockToken) && !string.IsNullOrWhiteSpace(cacheKey))
                {
                    try { await _routeCache.ReleaseLockAsync(cacheKey, lockToken); } catch { }
                }

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
                .Include(i => i.ItineraryDays)
                    .ThenInclude(d => d.Activities)
                        .ThenInclude(a => a.Transport)
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

            timeString = timeString.Trim()
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\n", "")
                .Replace("\r", "");

            if (string.IsNullOrEmpty(timeString))
                return null;

            try
            {
                if (TimeSpan.TryParse(timeString, System.Globalization.CultureInfo.InvariantCulture, out var time))
                    return time;

                var formats = new[] { "HH:mm", "H:mm", "hh:mm", "h:mm", "HH:mm:ss", "H:mm:ss", "h:mm tt", "hh:mm tt" };

                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(timeString,
                            format,
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out var dateTime))
                    {
                        return dateTime.TimeOfDay;
                    }
                }

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
