using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTripApi.Data;
using SmartTripApi.DTOs;
using SmartTripApi.Extensions;
using SmartTripApi.Services.GooglePlaces;
using SmartTripApi.Services.RoutePlanning;
using System.Security.Claims;

namespace SmartTripApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoutesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PlaceEnrichmentService _placeEnrichmentService;
        private readonly IRoutePlanService _routePlanService;
        private readonly ILogger<RoutesController> _logger;

        public RoutesController(
            AppDbContext context,
            PlaceEnrichmentService placeEnrichmentService,
            IRoutePlanService routePlanService,
            ILogger<RoutesController> logger)
        {
            _context = context;
            _placeEnrichmentService = placeEnrichmentService;
            _routePlanService = routePlanService;
            _logger = logger;
        }

        [HttpPost("plan")]
        [Authorize]
        public async Task<ActionResult<RoutePlanResponseDto>> CreateRoutePlan(
            [FromBody] RoutePlanRequestDto request)
        {
            var userId = User.GetUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid user token" });

            var result = await _routePlanService.CreateRoutePlanAsync(userId.Value, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.ErrorMessage });

            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<RoutePlanResponseDto>> GetRoutePlan(int id)
        {
            var userId = User.GetUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid user token" });

            var route = await _routePlanService.GetRoutePlanAsync(userId.Value, id);
            if (route == null)
                return NotFound(new { message = "Route plan not found" });

            return Ok(route);
        }

        [HttpGet("user")]
        [Authorize]
        public async Task<ActionResult<List<UserRouteSummaryDto>>> GetUserRoutePlans()
        {
            var userId = User.GetUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid user token" });

            var routes = await _routePlanService.GetUserRoutePlansAsync(userId.Value);
            return Ok(routes);
        }

        // EnrichExistingPlacesWithReviews SERVİSE TAŞINABİLİR 

        [HttpPost("enrich-reviews")]
        [Authorize]
        public async Task<ActionResult> EnrichExistingPlacesWithReviews()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var placesNeedingReviews = await _context.Places
                    .Where(p => !string.IsNullOrEmpty(p.GooglePlaceId))
                    .Where(p => !_context.GoogleReviews.Any(gr => gr.PlaceId == p.Id))
                    .ToListAsync();

                _logger.LogInformation("Found {Count} places needing review enrichment",
                    placesNeedingReviews.Count);

                int successCount = 0;
                int failCount = 0;

                foreach (var place in placesNeedingReviews)
                {
                    try
                    {
                        var success = await _placeEnrichmentService.EnrichPlaceReviewsAsync(place.Id);
                        if (success)
                            successCount++;
                        else
                            failCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        _logger.LogError(ex,
                            "Failed to enrich reviews for place {PlaceId} ({PlaceName})",
                            place.Id, place.Name);
                    }
                }

                return Ok(new
                {
                    message = "Review enrichment completed",
                    totalPlaces = placesNeedingReviews.Count,
                    successCount,
                    failCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during review enrichment");
                return StatusCode(500, new { message = "Failed to enrich reviews", error = ex.Message });
            }
        }
    }
}
