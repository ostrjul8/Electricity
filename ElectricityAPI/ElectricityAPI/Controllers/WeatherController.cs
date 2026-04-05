using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityAPI.Controllers
{
    [ApiController]
    [Route("api/weather")]
    [Authorize(Roles = "Admin")]
    public class WeatherController : ControllerBase
    {
        private readonly WeatherSyncService _weatherSyncService;
        private readonly ConsumptionSyncService _consumptionSyncService;

        public WeatherController(WeatherSyncService weatherSyncService, ConsumptionSyncService consumptionSyncService)
        {
            _weatherSyncService = weatherSyncService;
            _consumptionSyncService = consumptionSyncService;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncWeather()
        {
            try
            {
                await _weatherSyncService.SyncWeatherAsync();
                await _consumptionSyncService.SyncConsumptionAsync();

                await _consumptionSyncService.CleanupOldConsumptionAsync();

                await _weatherSyncService.CleanupOldWeatherAsync();

                return Ok(new { message = "Weather and consumption records synced successfully, old records were deleted." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("sync-window")]
        public async Task<IActionResult> SyncWindow()
        {
            try
            {
                await _weatherSyncService.EnsureWeatherWindowAsync();
                await _consumptionSyncService.EnsureConsumptionWindowAsync();

                return Ok(new { message = "Weather and consumption window synced: last 60 days plus today and next 2 days." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
