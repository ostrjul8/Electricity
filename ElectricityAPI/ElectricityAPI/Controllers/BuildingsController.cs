using DAL;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using BLL.Models;

namespace ElectricityAPI.Controllers
{
    [ApiController]
    [Route("api/buildings")]
    [Authorize(Roles = "User,Admin")]
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
            try
            {
                BuildingDetailsDTO? result = await _buildingQueryService.GetBuildingDetailsAsync(id);

                if (result is null)
                {
                    return NotFound(new { message = $"Building with id {id} not found." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("map-points")]
        public async Task<IActionResult> GetMapPoints()
        {
            try
            {
                List<BuildingMapPointDTO> points = await _buildingMapService.GetMapPointsAsync();

                string json = JsonSerializer.Serialize(points);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

                using MemoryStream output = new MemoryStream();
                using (BrotliStream brotli = new BrotliStream(output, CompressionLevel.Fastest, leaveOpen: true))
                {
                    await brotli.WriteAsync(jsonBytes, 0, jsonBytes.Length);
                }

                Response.Headers["Content-Encoding"] = "br";

                return File(output.ToArray(), "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                page = page < 1 ? 1 : page;
                pageSize = pageSize < 1 ? 10 : pageSize;

                PagedResultDTO<BuildingDTO> result = await _buildingQueryService.GetPagedBuildingsAsync(page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
