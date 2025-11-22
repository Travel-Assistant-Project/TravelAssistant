using Microsoft.AspNetCore.Mvc;
using SmartTripApi.DTOs;
using SmartTripApi.Services.AI;
using SmartTripApi.Data;
using System.Text.Json;

namespace SmartTripApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoutesController : ControllerBase
    {
        private readonly AIService _aiService;
        private readonly AppDbContext _db;

        public RoutesController(AIService aiService, AppDbContext db)
        {
            _aiService = aiService;
            _db = db;
        }

        // POST /api/routes/plan
        [HttpPost("plan")]
        public async Task<ActionResult<TripPlanResponse>> PlanRoute([FromBody] TripPlanRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 1) AI’den rota planı al
                var tripPlan = await _aiService.GenerateTripPlanAsync(request);

                // 2) DB’ye kaydedilecek entity oluştur
                var routeEntity = new RoutePlan
                {
                    Region = request.Region,
                    Days = request.Days,
                    Theme = request.Theme.ToString(),
                    Budget = request.Budget.ToString(),
                    Intensity = request.Intensity.ToString(),
                    Transport = request.Transport.ToString(),
                    PlanName = tripPlan.PlanName,
                    PlanJson = JsonSerializer.Serialize(tripPlan),
                    CreatedAt = DateTime.UtcNow
                };

                // 3) DB’ye kaydet
                _db.RoutePlans.Add(routeEntity);
                await _db.SaveChangesAsync();

                // 4) Kullanıcıya AI’nin ürettiği planı dön
                return Ok(tripPlan);
            }
            catch (Exception ex)
            {
                // Task’te istenen: try-catch + bad request
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
