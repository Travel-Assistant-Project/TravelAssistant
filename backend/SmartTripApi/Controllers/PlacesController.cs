using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTripApi.Data;
using SmartTripApi.DTOs;
using SmartTripApi.Extensions;
using SmartTripApi.Models;
using SmartTripApi.Services.GooglePlaces;
using System.Security.Claims;

namespace SmartTripApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlacesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PlacesController> _logger;
        private readonly GooglePlacesService _googlePlacesService;

        public PlacesController(
            AppDbContext context, 
            ILogger<PlacesController> logger,
            GooglePlacesService googlePlacesService)
        {
            _context = context;
            _logger = logger;
            _googlePlacesService = googlePlacesService;
        }

        [HttpGet("explore")]
        [Authorize]
        public async Task<ActionResult<List<PlaceDto>>> GetPlacesForExplore(
            [FromQuery] string? category = null,
            [FromQuery] int limit = 30)
        {
            try
            {
                var query = _context.Places.AsQueryable();

                // Category filtresi - case insensitive ve enum mapping
                if (!string.IsNullOrEmpty(category) && category != "All" && category.ToLower() != "all")
                {
                    // Frontend'den gelen category isimlerini enum'a çevir
                    var categoryLower = category.ToLower();
                    ThemeTypeEnum? categoryEnum = categoryLower switch
                    {
                        "nature" => ThemeTypeEnum.nature,
                        "sea" => ThemeTypeEnum.sea,
                        "history" => ThemeTypeEnum.history,
                        "beach" => ThemeTypeEnum.beach,
                        "food" => ThemeTypeEnum.food,
                        "photospot" => ThemeTypeEnum.photospot,
                        _ => null
                    };

                    if (categoryEnum.HasValue)
                    {
                        query = query.Where(p => p.Category == categoryEnum);
                        _logger.LogInformation("Filtering places by category: {Category} (enum: {Enum})", category, categoryEnum);
                    }
                    else
                    {
                        _logger.LogWarning("Unknown category: {Category}, returning all places", category);
                    }
                }
                else
                {
                    _logger.LogInformation("No category filter, returning all places (including null category)");
                }

                // Önce toplam sayıyı kontrol et (filtrelemeden önce)
                var totalCountBeforeFilter = await _context.Places.CountAsync();
                var totalCount = await query.CountAsync();
                _logger.LogInformation("Total places in DB: {Total}, Places found for category {Category}: {Count}", 
                    totalCountBeforeFilter, category ?? "All", totalCount);

                // Random sıralama ve limit
                var places = await query
                    .OrderBy(x => Guid.NewGuid()) // Random sıralama
                    .Take(limit)
                    .Select(p => new PlaceDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Category = p.Category.HasValue ? p.Category.Value.ToString() : null,
                        City = p.City,
                        Country = p.Country,
                        ImageUrls = p.PhotoUrls != null && p.PhotoUrls.Length > 0
                            ? p.PhotoUrls.ToList()
                            : null,
                        GoogleRating = p.GoogleRating,
                        Location = !string.IsNullOrEmpty(p.City) && !string.IsNullOrEmpty(p.Country)
                            ? $"{p.City}, {p.Country}"
                            : p.FormattedAddress ?? "Unknown location"
                    })
                    .ToListAsync();

                _logger.LogInformation("Returning {Count} places for category {Category}", places.Count, category);
                
                // Eğer kategori filtresi varsa ve sonuç yoksa, tüm kategorilerdeki toplam sayıyı logla
                if (!string.IsNullOrEmpty(category) && category.ToLower() != "all" && places.Count == 0)
                {
                    var allPlacesCount = await _context.Places.CountAsync();
                    var placesByCategory = await _context.Places
                        .GroupBy(p => p.Category)
                        .Select(g => new { Category = g.Key, Count = g.Count() })
                        .ToListAsync();
                    
                    var nullCategoryCount = await _context.Places.CountAsync(p => p.Category == null);
                    
                    _logger.LogWarning("No places found for category {Category}. Total places: {Total}, Null category: {NullCount}, Places by category: {ByCategory}", 
                        category, allPlacesCount, nullCategoryCount, string.Join(", ", placesByCategory.Select(p => $"{p.Category}: {p.Count}")));
                }

                return Ok(places);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching places for explore");
                return StatusCode(500, new { message = "Failed to fetch places", error = ex.Message });
            }
        }

        [HttpGet("location-info")]
        [Authorize]
        public async Task<ActionResult<LocationInfoDto>> GetLocationInfo([FromQuery] string destination)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(destination))
                {
                    return BadRequest(new { message = "Destination is required" });
                }

                _logger.LogInformation("Getting location info for destination: {Destination}", destination);

                // Search for the place using Google Places Text Search
                var placeResult = await _googlePlacesService.SearchPlaceAsync(destination);

                if (placeResult == null || string.IsNullOrEmpty(placeResult.PlaceId))
                {
                    _logger.LogWarning("No place found for destination: {Destination}", destination);
                    return NotFound(new { message = "Location not found" });
                }

                // Get place details with address components
                var placeDetails = await _googlePlacesService.GetPlaceDetailsWithAddressAsync(placeResult.PlaceId);

                if (placeDetails == null)
                {
                    _logger.LogWarning("Could not get place details for destination: {Destination}", destination);
                    return NotFound(new { message = "Could not retrieve location details" });
                }

                // Extract country and city from address components
                var locationInfo = _googlePlacesService.ExtractLocationInfo(placeDetails);

                if (locationInfo == null)
                {
                    _logger.LogWarning("Could not extract location info for destination: {Destination}", destination);
                    return NotFound(new { message = "Could not extract location information" });
                }

                _logger.LogInformation("Location info extracted - Country: {Country}, City: {City}", 
                    locationInfo.Country, locationInfo.City);

                return Ok(locationInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location info for destination: {Destination}", destination);
                return StatusCode(500, new { message = "Failed to get location info", error = ex.Message });
            }
        }
    }
}

