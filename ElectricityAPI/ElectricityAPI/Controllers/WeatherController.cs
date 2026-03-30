using BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityAPI.Controllers
{
    [ApiController]
    [Route("api/weather")]
    public class WeatherController : ControllerBase
    {
        private readonly WeatherSyncService _weatherSyncService;

        public WeatherController(WeatherSyncService weatherSyncService)
        {
            _weatherSyncService = weatherSyncService;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncWeather()
        {
            try
            {
                await _weatherSyncService.SyncWeatherAsync();

                await _weatherSyncService.CleanupOldWeatherAsync();

                return Ok(new { message = "Погоду успішно синхронізовано, старі записи видалено." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
