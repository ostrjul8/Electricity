using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityAPI.Controllers
{
    [ApiController]
    [Route("api/forecast")]
    [Authorize(Roles = "Admin")]
    public class ForecastController : ControllerBase
    {
        private readonly ForecastScriptService _forecastScriptService;

        public ForecastController(ForecastScriptService forecastScriptService)
        {
            _forecastScriptService = forecastScriptService;
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunForecastScript()
        {
            try
            {
                await _forecastScriptService.RunForecastScriptAsync();
                await _forecastScriptService.CleanupOldForecastsAsync();

                return Ok(new { message = "Forecast script executed successfully. Old forecast archives were deleted." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
