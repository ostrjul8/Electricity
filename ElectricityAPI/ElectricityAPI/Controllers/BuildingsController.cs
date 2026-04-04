using DAL;
using BLL.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace ElectricityAPI.Controllers
{
    [ApiController]
    [Route("api/buildings")]
    public class BuildingsController : ControllerBase
    {
        private readonly BuildingQueryService _buildingQueryService;
        private readonly BuildingMapService _buildingMapService;

        public BuildingsController(BuildingQueryService buildingQueryService, BuildingMapService buildingMapService)
        {
            _buildingQueryService = buildingQueryService;
            _buildingMapService = buildingMapService;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _buildingQueryService.GetBuildingDetailsAsync(id);

            if (result is null)
            {
                return NotFound(new { message = $"Building with id {id} not found." });
            }

            return Ok(result);
        }

        [HttpGet("map-points")]
        public async Task<IActionResult> GetMapPoints()
        {
            var points = await _buildingMapService.GetMapPointsAsync();

            var json = JsonSerializer.Serialize(points);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            using var output = new MemoryStream();
            using (var brotli = new BrotliStream(output, CompressionLevel.Fastest, leaveOpen: true))
            {
                await brotli.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            }

            Response.Headers["Content-Encoding"] = "br";

            return File(output.ToArray(), "application/json");
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _buildingQueryService.GetPagedBuildingsAsync(page, pageSize);
            return Ok(result);
        }
    }
}
