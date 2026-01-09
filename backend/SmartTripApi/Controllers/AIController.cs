using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SmartTripApi.DTOs;
using SmartTripApi.Services.AI;
using SmartTripApi.Services;
using SmartTripApi.Extensions;

namespace SmartTripApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIController : ControllerBase
    {
        private readonly AIService _aiService;
        private readonly UserAnalysisService _userAnalysisService;

        public AIController(AIService aiService, UserAnalysisService userAnalysisService)
        {
            _aiService = aiService;
            _userAnalysisService = userAnalysisService;
        }

        [HttpPost("generate-trip")]
        public async Task<ActionResult<TripPlanResponse>> GenerateTrip(TripPlanRequest request)
        {
            try
            {
                var tripPlan = await _aiService.GenerateTripPlanAsync(request);
                return Ok(tripPlan);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get personalized recommendations based on user's favorite trips and places
        /// </summary>
        [HttpGet("user-recommendations")]
        [Authorize]
        public async Task<ActionResult<UserAnalysisResult>> GetUserRecommendations()
        {
            var userId = User.GetUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid user token" });

            try
            {
                var analysis = await _userAnalysisService.AnalyzeUserPreferencesAsync(userId.Value);
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "AI Service is working", timestamp = DateTime.Now });
        }

        [HttpGet("list-models")]
        public async Task<IActionResult> ListModels()
        {
            try
            {
                var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                var response = await _aiService._httpClient.GetAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}");
                
                var content = await response.Content.ReadAsStringAsync();
                return Ok(new { models = content });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
