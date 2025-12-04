using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartTripApi.Data;
using SmartTripApi.Models;
using SmartTripApi.Helpers;

namespace SmartTripApi.Services.GooglePlaces
{
    /// <summary>
    /// Service to enrich place data with Google Places API information
    /// </summary>
    public class PlaceEnrichmentService
    {
        private readonly AppDbContext _context;
        private readonly GooglePlacesService _googlePlacesService;
        private readonly ILogger<PlaceEnrichmentService> _logger;

        public PlaceEnrichmentService(
            AppDbContext context, 
            GooglePlacesService googlePlacesService,
            ILogger<PlaceEnrichmentService> logger)
        {
            _context = context;
            _googlePlacesService = googlePlacesService;
            _logger = logger;
        }

        /// <summary>
        /// Fetch and insert Google reviews for a place that already has google_place_id
        /// </summary>
        public async Task<bool> EnrichPlaceReviewsAsync(int placeId)
        {
            try
            {
                var place = await _context.Places.FindAsync(placeId);
                
                if (place == null)
                {
                    _logger.LogWarning("Place {PlaceId} not found", placeId);
                    return false;
                }

                if (string.IsNullOrEmpty(place.GooglePlaceId))
                {
                    _logger.LogWarning("Place {PlaceId} ({PlaceName}) has no google_place_id", 
                        place.Id, place.Name);
                    return false;
                }

                // Check if reviews already exist
                var existingReviewsCount = await _context.GoogleReviews
                    .CountAsync(gr => gr.PlaceId == placeId);

                if (existingReviewsCount > 0)
                {
                    _logger.LogInformation("Place {PlaceId} already has {Count} reviews, skipping", 
                        placeId, existingReviewsCount);
                    return true;
                }

                _logger.LogInformation("Fetching reviews for place {PlaceId} ({PlaceName})", 
                    place.Id, place.Name);
                
                var placeDetails = await _googlePlacesService.GetPlaceDetailsAsync(place.GooglePlaceId);
                
                if (placeDetails?.Reviews != null && placeDetails.Reviews.Any())
                {
                    await InsertGoogleReviewsAsync(place.Id, placeDetails.Reviews);
                    _logger.LogInformation("Successfully enriched reviews for place {PlaceId} ({PlaceName})", 
                        place.Id, place.Name);
                    return true;
                }
                else
                {
                    _logger.LogWarning("No reviews found for place {PlaceId} ({PlaceName})", 
                        place.Id, place.Name);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enriching reviews for place {PlaceId}", placeId);
                return false;
            }
        }

        /// <summary>
        /// Enrich all places in an itinerary with Google Places data
        /// </summary>
        public async Task EnrichItineraryPlacesAsync(int itineraryId)
        {
            try
            {
                _logger.LogInformation("Starting place enrichment for itinerary {ItineraryId}", itineraryId);

                // Get itinerary with all days and activities
                var itinerary = await _context.Itineraries
                    .Include(i => i.ItineraryDays)
                        .ThenInclude(d => d.Activities)
                            .ThenInclude(a => a.Place)
                    .FirstOrDefaultAsync(i => i.Id == itineraryId);

                if (itinerary == null)
                {
                    _logger.LogWarning("Itinerary {ItineraryId} not found", itineraryId);
                    return;
                }

                // Extract all unique places that need enrichment
                var placesToEnrich = itinerary.ItineraryDays
                    .SelectMany(d => d.Activities)
                    .Where(a => a.Place != null && string.IsNullOrEmpty(a.Place.GooglePlaceId))
                    .Select(a => a.Place!)
                    .DistinctBy(p => p.Id)
                    .ToList();

                _logger.LogInformation("Found {Count} places to enrich", placesToEnrich.Count);

                foreach (var place in placesToEnrich)
                {
                    await EnrichPlaceAsync(place, itinerary.Region);
                }

                _logger.LogInformation("Completed place enrichment for itinerary {ItineraryId}", itineraryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enriching places for itinerary {ItineraryId}", itineraryId);
                throw;
            }
        }

        /// <summary>
        /// Enrich a single place with Google Places data
        /// </summary>
        public async Task<bool> EnrichPlaceAsync(Place place, string? region = null)
        {
            try
            {
                // Skip if already enriched
                if (!string.IsNullOrEmpty(place.GooglePlaceId))
                {
                    _logger.LogInformation("Place {PlaceId} ({PlaceName}) already enriched, skipping", 
                        place.Id, place.Name);
                    return true;
                }

                _logger.LogInformation("Enriching place: {PlaceName} in region: {Region}", 
                    place.Name, region ?? place.City);

                // Search Google Places
                var searchRegion = region ?? place.City ?? place.Country;
                var googlePlace = await _googlePlacesService.SearchPlaceAsync(place.Name, searchRegion);

                if (googlePlace == null)
                {
                    _logger.LogWarning("No Google Places data found for: {PlaceName}", place.Name);
                    return false;
                }

                // Update place with Google data
                place.GooglePlaceId = googlePlace.PlaceId;
                place.FormattedAddress = googlePlace.FormattedAddress;
                place.Latitude = googlePlace.Geometry?.Location?.Lat;
                place.Longitude = googlePlace.Geometry?.Location?.Lng;
                place.GoogleRating = googlePlace.Rating;
                place.UserRatingsTotal = googlePlace.UserRatingsTotal;
                place.PriceLevel = googlePlace.PriceLevel;
                
                // Store opening hours as JSON
                if (googlePlace.OpeningHours != null)
                {
                    place.OpeningHours = JsonSerializer.Serialize(googlePlace.OpeningHours);
                }

                // Generate Google Maps URL
                place.GoogleMapsUrl = _googlePlacesService.GetGoogleMapsUrl(googlePlace.PlaceId);

                // Get photo URLs (2-3 photos)
                var photoUrls = _googlePlacesService.GetPhotoUrls(googlePlace, count: 3, maxWidth: 800);
                place.PhotoUrls = photoUrls.ToArray();

                // Save place updates
                await _context.SaveChangesAsync();

                // Insert photos into place_photos table
                if (photoUrls.Any())
                {
                    await InsertPlacePhotosAsync(place.Id, photoUrls);
                }

                // Fetch and insert reviews from Place Details API (2-3 reviews)
                _logger.LogInformation("Fetching reviews for place {PlaceId} ({PlaceName})", 
                    place.Id, place.Name);
                
                var placeDetails = await _googlePlacesService.GetPlaceDetailsAsync(googlePlace.PlaceId);
                if (placeDetails?.Reviews != null && placeDetails.Reviews.Any())
                {
                    await InsertGoogleReviewsAsync(place.Id, placeDetails.Reviews);
                }
                else
                {
                    _logger.LogWarning("No reviews found for place {PlaceId} ({PlaceName})", 
                        place.Id, place.Name);
                }

                _logger.LogInformation("Successfully enriched place {PlaceId} ({PlaceName}) with Google data", 
                    place.Id, place.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enriching place {PlaceId} ({PlaceName})", 
                    place.Id, place.Name);
                return false;
            }
        }

        /// <summary>
        /// Insert photos into place_photos table
        /// </summary>
        private async Task InsertPlacePhotosAsync(int placeId, List<string> photoUrls)
        {
            try
            {
                foreach (var photoUrl in photoUrls)
                {
                    // Check if photo already exists (avoid duplicates)
                    var exists = await _context.PlacePhotos
                        .AnyAsync(p => p.PlaceId == placeId && p.ImageUrl == photoUrl);

                    if (!exists)
                    {
                        var placePhoto = new PlacePhoto
                        {
                            PlaceId = placeId,
                            ImageUrl = photoUrl,
                            CreatedAt = DateTimeHelper.GetTurkeyTime()
                        };

                        _context.PlacePhotos.Add(placePhoto);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Inserted {Count} photos for place {PlaceId}", 
                    photoUrls.Count, placeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting photos for place {PlaceId}", placeId);
                throw;
            }
        }

        /// <summary>
        /// Insert Google reviews into google_reviews table
        /// </summary>
        private async Task InsertGoogleReviewsAsync(int placeId, List<DTOs.GoogleReview> googleReviews)
        {
            try
            {
                // Take only first 2-3 reviews
                var reviewsToInsert = googleReviews.Take(3).ToList();

                foreach (var googleReview in reviewsToInsert)
                {
                    // Check if review already exists (avoid duplicates based on author and text)
                    var exists = await _context.GoogleReviews
                        .AnyAsync(r => r.PlaceId == placeId 
                            && r.AuthorName == googleReview.AuthorName 
                            && r.Comment == googleReview.Text);

                    if (!exists)
                    {
                        var review = new GoogleReview
                        {
                            PlaceId = placeId,
                            AuthorName = googleReview.AuthorName,
                            Comment = googleReview.Text,
                            Rating = googleReview.Rating,
                            ProfilePhotoUrl = googleReview.ProfilePhotoUrl,
                            ReviewTime = googleReview.Time.HasValue 
                                ? DateTimeOffset.FromUnixTimeSeconds(googleReview.Time.Value).DateTime
                                : null,
                            CreatedAt = DateTimeHelper.GetTurkeyTime()
                        };

                        _context.GoogleReviews.Add(review);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Inserted {Count} Google reviews for place {PlaceId}", 
                    reviewsToInsert.Count, placeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting Google reviews for place {PlaceId}", placeId);
                throw;
            }
        }
    }
}
