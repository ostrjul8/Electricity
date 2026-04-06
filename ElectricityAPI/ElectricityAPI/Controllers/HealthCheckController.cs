using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace ElectricityAPI.Controllers
{
    [ApiController]
    [Route("api/v1/health")]
    [AllowAnonymous]
    public class HealthCheckController : ControllerBase
    {
        /// <summary>
        /// Перевіряє стан працездатності API.
        /// </summary>
        /// <returns>Статус API та поточний час.</returns>
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                return Ok(new
                {
                    status = "Healthy",
                    timestamp = KyivTimeHelper.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}