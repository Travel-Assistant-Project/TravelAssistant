using Microsoft.AspNetCore.Mvc;
using SmartTripApi.DTOs;
using SmartTripApi.Services.AI;

namespace SmartTripApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIController : ControllerBase
    {
        private readonly AIService _aiService;

        public AIController(AIService aiService)
        {
            _aiService = aiService;
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
