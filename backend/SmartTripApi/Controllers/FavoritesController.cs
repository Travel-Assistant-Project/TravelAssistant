using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTripApi.Data;
using SmartTripApi.Extensions;
using SmartTripApi.Models;
using SmartTripApi.Helpers;

namespace SmartTripApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FavoritesController> _logger;

        public FavoritesController(AppDbContext context, ILogger<FavoritesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all favorite itineraries for the current user
        /// </summary>
        [HttpGet("itineraries")]
        public async Task<ActionResult<List<object>>> GetFavoriteItineraries()
        {
            var userId = User.GetUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid user token" });

            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId.Value && f.ItineraryId != null)
                .Include(f => f.Itinerary)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new
                {
                    id = f.Itinerary!.Id,
                    name = f.Itinerary.Name,
                    region = f.Itinerary.Region,
                    daysCount = f.Itinerary.DaysCount,
                    theme = f.Itinerary.Theme,
                    budget = f.Itinerary.Budget,
                    intensity = f.Itinerary.Intensity,
                    transport = f.Itinerary.Transport,
                    isAiGenerated = f.Itinerary.IsAiGenerated,
                    createdAt = f.Itinerary.CreatedAt
                })
                .ToListAsync();

            return Ok(favorites);
        }

        /// <summary>
        /// Add an itinerary to favorites
        /// </summary>
        [HttpPost("itineraries/{itineraryId}")]
        public async Task<ActionResult> AddFavoriteItinerary(int itineraryId)
        {
            var userId = User.GetUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid user token" });

            // Check if itinerary exists
            var itinerary = await _context.Itineraries
                .FirstOrDefaultAsync(i => i.Id == itineraryId && i.UserId == userId.Value);

            if (itinerary == null)
                return NotFound(new { message = "Itinerary not found" });

            // Check if already favorited
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.ItineraryId == itineraryId);

            if (existingFavorite != null)
                return Ok(new { message = "Already in favorites" });

            var favorite = new Favorite
            {
                UserId = userId.Value,
                ItineraryId = itineraryId,
                CreatedAt = DateTimeHelper.GetTurkeyTime()
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Added to favorites", id = favorite.Id });
        }

        /// <summary>
        /// Remove an itinerary from favorites
        /// </summary>
        [HttpDelete("itineraries/{itineraryId}")]
        public async Task<ActionResult> RemoveFavoriteItinerary(int itineraryId)
        {
            var userId = User.GetUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid user token" });

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.ItineraryId == itineraryId);

            if (favorite == null)
                return NotFound(new { message = "Favorite not found" });

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Removed from favorites" });
        }

        /// <summary>
        /// Check if an itinerary is favorited
        /// </summary>
        [HttpGet("itineraries/{itineraryId}/check")]
        public async Task<ActionResult<bool>> CheckFavoriteItinerary(int itineraryId)
        {
            var userId = User.GetUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid user token" });

            var isFavorite = await _context.Favorites
                .AnyAsync(f => f.UserId == userId.Value && f.ItineraryId == itineraryId);

            return Ok(isFavorite);
        }

        /// <summary>
        /// Get all favorite itinerary IDs for the current user
        /// </summary>
        [HttpGet("itineraries/ids")]
        public async Task<ActionResult<List<int>>> GetFavoriteItineraryIds()
        {
            var userId = User.GetUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid user token" });

            var favoriteIds = await _context.Favorites
                .Where(f => f.UserId == userId.Value && f.ItineraryId != null)
                .Select(f => f.ItineraryId!.Value)
                .ToListAsync();

            return Ok(favoriteIds);
        }
    }
}

