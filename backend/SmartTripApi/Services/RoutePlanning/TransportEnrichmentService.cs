using Microsoft.EntityFrameworkCore;
using SmartTripApi.Data;
using SmartTripApi.Models;
using SmartTripApi.Services.GooglePlaces;
using SmartTripApi.DTOs;
using System.Text.Json;

namespace SmartTripApi.Services.RoutePlanning
{
    public class TransportEnrichmentService
    {
        private readonly AppDbContext _context;
        private readonly GooglePlacesService _googlePlacesService;
        private readonly ILogger<TransportEnrichmentService> _logger;

        public TransportEnrichmentService(
            AppDbContext context,
            GooglePlacesService googlePlacesService,
            ILogger<TransportEnrichmentService> logger)
        {
            _context = context;
            _googlePlacesService = googlePlacesService;
            _logger = logger;
        }

        public async Task EnrichItineraryTransportAsync(int itineraryId)
        {
            var itinerary = await _context.Itineraries
                .Include(i => i.ItineraryDays)
                .ThenInclude(d => d.Activities)
                .ThenInclude(a => a.Place)
                .Include(i => i.ItineraryDays)
                .ThenInclude(d => d.Activities)
                .ThenInclude(a => a.Transport) // Include transport details
                .FirstOrDefaultAsync(i => i.Id == itineraryId);

            if (itinerary == null) return;

            // Transport modunu belirle
            var transportMode = "transit"; // Default
            
            // Eğer kullanıcı özellikle araba veya yürüyüş seçtiyse onu kullan
            if (itinerary.Transport == TransportModeEnum.car)
            {
                transportMode = "driving";
            }
            else if (itinerary.Transport == TransportModeEnum.walk)
            {
                transportMode = "walking";
            }

            _logger.LogInformation("Enriching itinerary {ItineraryId} with transport mode {Mode}. Days: {DayCount}", 
                itineraryId, transportMode, itinerary.ItineraryDays.Count);

            foreach (var day in itinerary.ItineraryDays.OrderBy(d => d.DayNumber))
            {
                var activities = day.Activities.OrderBy(a => a.StartTime).ToList();
                _logger.LogInformation("Day {DayNumber} has {ActivityCount} activities", day.DayNumber, activities.Count);

                for (int i = 1; i < activities.Count; i++)
                {
                    var prevActivity = activities[i - 1];
                    var currentActivity = activities[i];

                    if (prevActivity.Place?.GooglePlaceId == null || currentActivity.Place?.GooglePlaceId == null)
                    {
                        _logger.LogWarning("Skipping transport due to missing GooglePlaceId. Prev: {PrevId}, Curr: {CurrId}", 
                            prevActivity.Place?.GooglePlaceId, currentActivity.Place?.GooglePlaceId);
                        continue;
                    }

                    _logger.LogInformation("Fetching directions from {Origin} to {Dest}", prevActivity.Place.Name, currentActivity.Place.Name);

                    var directions = await _googlePlacesService.GetDirectionsAsync(
                        prevActivity.Place.GooglePlaceId,
                        currentActivity.Place.GooglePlaceId,
                        transportMode);

                    // FALLBACK LOGIC: If transit fails, try driving
                    bool usedFallback = false;
                    if ((directions == null || directions.Routes == null || !directions.Routes.Any() || directions.Status == "ZERO_RESULTS") && transportMode == "transit")
                    {
                        _logger.LogWarning("Transit directions not found. Trying DRIVING as fallback...");
                        var fallbackDirections = await _googlePlacesService.GetDirectionsAsync(
                            prevActivity.Place.GooglePlaceId,
                            currentActivity.Place.GooglePlaceId,
                            "driving");
                        
                        if (fallbackDirections != null && fallbackDirections.Routes != null && fallbackDirections.Routes.Any())
                        {
                            directions = fallbackDirections;
                            usedFallback = true;
                        }
                    }

                    if (directions?.Routes != null && directions.Routes.Any())
                    {
                        _logger.LogInformation("Directions found! Route count: {RouteCount}", directions.Routes.Count);
                        
                        var route = directions.Routes.First();
                        var leg = route.Legs.FirstOrDefault();

                        if (leg != null)
                        {
                            _logger.LogInformation("Saving transport details for Activity {ActivityId}. Duration: {Duration}", currentActivity.Id, leg.Duration?.Text);

                            // Kaydederken "public_transport" string'ini kullanıyoruz frontend uyumu için
                            string modeForDb = "public_transport";
                            if (transportMode == "driving") modeForDb = "driving";
                            if (transportMode == "walking") modeForDb = "walking";
                            
                            // If we used fallback, save as driving so frontend renders it green
                            if (usedFallback) 
                            {
                                modeForDb = "driving";
                            }

                            var stepsSummary = ExtractTransitSummary(leg);
                            var stepsJson = JsonSerializer.Serialize(stepsSummary);
                            var polyline = route.OverviewPolyline?.Points;
                            var durationMinutes = (int?)Math.Ceiling((double)(leg.Duration?.Value ?? 0) / 60);
                            var distanceMeters = leg.Distance?.Value;

                            // 1. Legacy fields
                            currentActivity.TravelFromPreviousMode = modeForDb;
                            currentActivity.TravelFromPreviousDurationMinutes = durationMinutes;
                            currentActivity.TravelFromPreviousDistanceMeters = distanceMeters;
                            currentActivity.TravelFromPreviousPolyline = polyline;
                            currentActivity.TravelFromPreviousDetailsJson = stepsJson;

                            // 2. New ActivityTransport Table - DIRECT INSERT STRATEGY
                            var existingTransport = await _context.ActivityTransports
                                .FirstOrDefaultAsync(t => t.ActivityId == currentActivity.Id);

                            if (existingTransport == null)
                            {
                                var newTransport = new ActivityTransport 
                                { 
                                    ActivityId = currentActivity.Id,
                                    Mode = modeForDb,
                                    DurationMinutes = durationMinutes,
                                    DistanceMeters = distanceMeters,
                                    PolylinePoints = polyline,
                                    DetailsJson = stepsJson,
                                    CreatedAt = DateTime.UtcNow
                                };
                                _context.ActivityTransports.Add(newTransport);
                                _logger.LogInformation("Adding NEW transport record for Activity {Id}", currentActivity.Id);
                            }
                            else
                            {
                                existingTransport.Mode = modeForDb;
                                existingTransport.DurationMinutes = durationMinutes;
                                existingTransport.DistanceMeters = distanceMeters;
                                existingTransport.PolylinePoints = polyline;
                                existingTransport.DetailsJson = stepsJson;
                                _context.ActivityTransports.Update(existingTransport);
                                _logger.LogInformation("Updating EXISTING transport record for Activity {Id}", currentActivity.Id);
                            }
                            
                            // Save changes immediately and detach to avoid tracking issues
                            await _context.SaveChangesAsync();
                            _context.Entry(currentActivity).State = EntityState.Detached; // Detach to be safe
                            
                            _logger.LogInformation("SAVED successfully to DB!");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No directions found. Status: {Status}", directions?.Status);
                    }
                    
                    // API rate limiting
                    await Task.Delay(200);
                }
            }
            // await _context.SaveChangesAsync(); // Moved inside loop
        }

        private object ExtractTransitSummary(GoogleDirectionLeg leg)
        {
            var steps = new List<object>();

            foreach (var step in leg.Steps)
            {
                if (step.TravelMode == "TRANSIT" && step.TransitDetails != null)
                {
                    steps.Add(new
                    {
                        mode = "transit",
                        // Hat numarası yoksa, araç ismini (Bus, Tram) kullan
                        line = step.TransitDetails.Line?.ShortName 
                               ?? step.TransitDetails.Line?.Name 
                               ?? step.TransitDetails.Line?.Vehicle?.Name, 
                        vehicle = step.TransitDetails.Line?.Vehicle?.Type,
                        departure = step.TransitDetails.DepartureStop?.Name,
                        arrival = step.TransitDetails.ArrivalStop?.Name,
                        stops = step.TransitDetails.NumStops,
                        duration = step.Duration?.Text
                    });
                }
                else if (step.TravelMode == "WALKING")
                {
                     steps.Add(new
                    {
                        mode = "walking",
                        duration = step.Duration?.Text,
                        distance = step.Distance?.Text
                    });
                }
                 else if (step.TravelMode == "DRIVING")
                {
                     steps.Add(new
                    {
                        mode = "driving",
                        duration = step.Duration?.Text,
                        distance = step.Distance?.Text
                    });
                }
            }

            return steps;
        }
    }
}

