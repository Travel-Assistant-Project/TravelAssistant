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

            _logger.LogInformation("Enriching itinerary {ItineraryId} with intelligent transport selection. Days: {DayCount}", 
                itineraryId, itinerary.ItineraryDays.Count);

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

                    // Get user's selected transport modes
                    var selectedTransports = itinerary.GetSelectedTransportModes();
                    _logger.LogInformation("User selected transport modes: {Modes}", string.Join(", ", selectedTransports));

                    // Smart transport selection based on distance and user preferences
                    var smartTransportResult = await GetSmartTransportDirections(
                        prevActivity.Place.GooglePlaceId,
                        currentActivity.Place.GooglePlaceId,
                        selectedTransports);

                    var directions = smartTransportResult.directions;
                    var finalTransportMode = smartTransportResult.finalMode;

                    if (directions?.Routes != null && directions.Routes.Any())
                    {
                        _logger.LogInformation("Directions found! Route count: {RouteCount}, Final mode: {Mode}", directions.Routes.Count, finalTransportMode);
                        
                        var route = directions.Routes.First();
                        var leg = route.Legs.FirstOrDefault();

                        if (leg != null)
                        {
                            _logger.LogInformation("Saving transport details for Activity {ActivityId}. Duration: {Duration}, Mode: {Mode}", 
                                currentActivity.Id, leg.Duration?.Text, finalTransportMode);

                            // Map transport mode to database string
                            string modeForDb = finalTransportMode switch
                            {
                                "driving" => "driving",
                                "walking" => "walking", 
                                "transit" => "public_transport",
                                _ => "public_transport"
                            };

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
                        _logger.LogWarning("No directions found for transport between {Origin} and {Destination}", 
                            prevActivity.Place.Name, currentActivity.Place.Name);
                    }
                    
                    // API rate limiting
                    await Task.Delay(200);
                }
            }
            // await _context.SaveChangesAsync(); // Moved inside loop
        }

        private async Task<(GoogleDirectionsResponse? directions, string finalMode)> GetSmartTransportDirections(
            string originPlaceId, 
            string destinationPlaceId, 
            List<string> selectedTransports)
        {
            _logger.LogInformation("GetSmartTransportDirections called with transports: {Transports}", string.Join(", ", selectedTransports));
            
            // Step 1: Always check walking distance first to determine if it's feasible
            var walkingDirections = await _googlePlacesService.GetDirectionsAsync(
                originPlaceId, destinationPlaceId, "walking");

            bool isWalkingFeasible = false;
            int walkingDurationMinutes = 0;

            if (walkingDirections?.Routes != null && walkingDirections.Routes.Any())
            {
                var walkingLeg = walkingDirections.Routes.First().Legs.FirstOrDefault();
                if (walkingLeg?.Duration?.Value != null)
                {
                    walkingDurationMinutes = (int)Math.Ceiling((double)walkingLeg.Duration.Value / 60);
                    isWalkingFeasible = walkingDurationMinutes <= 30; // 30 minutes or less
                }
            }

            _logger.LogInformation("Walking duration: {WalkingDuration} minutes, feasible: {IsFeasible}", 
                walkingDurationMinutes, isWalkingFeasible);

            // Step 2: Use smart transport selection based on user's choices
            var selectedTransportMode = DetermineSmartTransport(selectedTransports, walkingDurationMinutes);
            _logger.LogInformation("Smart transport decision: {Mode}", selectedTransportMode);
            
            // Step 3: Get directions for the selected transport mode
            GoogleDirectionsResponse? finalDirections = null;
            string finalMode = selectedTransportMode;
            
            switch (selectedTransportMode.ToLower())
            {
                case "walking":
                    finalDirections = walkingDirections;
                    break;
                    
                case "driving":
                    finalDirections = await _googlePlacesService.GetDirectionsAsync(
                        originPlaceId, destinationPlaceId, "driving");
                    break;
                    
                case "transit":
                    finalDirections = await _googlePlacesService.GetDirectionsAsync(
                        originPlaceId, destinationPlaceId, "transit");
                    break;
            }

            // Step 4: Fallback logic if the selected mode doesn't work
            if (finalDirections?.Routes == null || !finalDirections.Routes.Any() || finalDirections.Status == "ZERO_RESULTS")
            {
                _logger.LogWarning("Selected transport mode {Mode} failed, trying fallback options", selectedTransportMode);
                
                // Try walking first as fallback
                if (walkingDirections?.Routes != null && walkingDirections.Routes.Any())
                {
                    _logger.LogInformation("Using walking as fallback");
                    return (walkingDirections, "walking");
                }
                
                // Then try driving
                var drivingFallback = await _googlePlacesService.GetDirectionsAsync(
                    originPlaceId, destinationPlaceId, "driving");
                if (drivingFallback?.Routes != null && drivingFallback.Routes.Any())
                {
                    _logger.LogInformation("Using driving as fallback");
                    return (drivingFallback, "driving");
                }
                
                // Finally try transit
                var transitFallback = await _googlePlacesService.GetDirectionsAsync(
                    originPlaceId, destinationPlaceId, "transit");
                if (transitFallback?.Routes != null && transitFallback.Routes.Any())
                {
                    _logger.LogInformation("Using transit as fallback");
                    return (transitFallback, "transit");
                }
            }

            _logger.LogInformation("Final transport mode: {Mode}", finalMode);
            return (finalDirections, finalMode);
        }

        private string DetermineSmartTransport(List<string> selectedTransports, double? durationMinutes)
        {
            _logger.LogInformation("DetermineSmartTransport - Selected: [{Transports}], Duration: {Duration}min", 
                string.Join(", ", selectedTransports ?? new List<string>()), durationMinutes);
            
            // If no specific transports selected, use default logic
            if (selectedTransports == null || !selectedTransports.Any())
            {
                var result = durationMinutes <= 30 ? "walking" : "transit";
                _logger.LogInformation("No transports selected, using default: {Result}", result);
                return result;
            }

            // If only one transport mode selected, use it directly
            if (selectedTransports.Count == 1)
            {
                var result = MapTransportMode(selectedTransports[0]);
                _logger.LogInformation("Single transport selected: {Selected} -> {Result}", selectedTransports[0], result);
                return result;
            }

            // Multiple transport modes selected - use smart selection based on duration
            bool hasWalking = selectedTransports.Any(t => t.Equals("walk", StringComparison.OrdinalIgnoreCase));
            bool hasCar = selectedTransports.Any(t => t.Equals("car", StringComparison.OrdinalIgnoreCase));
            bool hasPublicTransport = selectedTransports.Any(t => 
                t.Equals("public_transport", StringComparison.OrdinalIgnoreCase) || 
                t.Equals("Public Transport", StringComparison.OrdinalIgnoreCase));

            _logger.LogInformation("Transport availability - Walking: {Walking}, Car: {Car}, Public: {Public}", 
                hasWalking, hasCar, hasPublicTransport);

            // For short distances (30 minutes or less), prefer walking if available
            if (durationMinutes <= 30 && hasWalking)
            {
                _logger.LogInformation("Short distance ({Duration}min) + walking available -> walking", durationMinutes);
                return "walking";
            }

            // For longer distances, prefer car over public transport if both are available
            if (durationMinutes > 30)
            {
                if (hasCar)
                {
                    _logger.LogInformation("Long distance ({Duration}min) + car available -> driving", durationMinutes);
                    return "driving";
                }
                if (hasPublicTransport)
                {
                    _logger.LogInformation("Long distance ({Duration}min) + public transport available -> transit", durationMinutes);
                    return "transit";
                }
            }

            // Fallback: return the first available transport mode
            var fallback = MapTransportMode(selectedTransports[0]);
            _logger.LogInformation("Using fallback: {Selected} -> {Result}", selectedTransports[0], fallback);
            return fallback;
        }

        private string MapTransportMode(string transportMode)
        {
            return transportMode?.ToLower() switch
            {
                "car" => "driving",
                "walk" => "walking",
                "public transport" => "transit",
                "public_transport" => "transit", // Handle both formats
                _ => "walking" // Default fallback
            };
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

