using Microsoft.AspNetCore.Mvc;
// DENEME İÇİN YAZILMIŞTIR
namespace SmartTripApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripController : ControllerBase
    {
        [HttpGet("hello")]
        public IActionResult GetHello()
        {
            return Ok(new { message = "SmartTrip .NET API çalışıyor 🚀" });
        }
    }
}
