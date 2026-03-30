using Microsoft.AspNetCore.Mvc;
using System;

namespace ElectricityAPI.Controllers
{
    [ApiController]
    [Route("api/v1/health")]
    public class HealthCheckController : ControllerBase
    {
        /// <summary>
        /// Перевіряє стан працездатності API.
        /// </summary>
        /// <returns>Статус API та поточний час.</returns>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new 
            { 
                status = "Healthy", 
                timestamp = DateTime.UtcNow 
            });
        }
    }
}