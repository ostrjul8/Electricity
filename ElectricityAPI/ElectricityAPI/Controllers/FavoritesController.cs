using BLL.Models;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectricityAPI.Controllers
{
    [ApiController]
    [Route("api/favorites")]
    [Authorize(Roles = "User,Admin")]
    public class FavoritesController : ControllerBase
    {
        private readonly FavoriteService _favoriteService;

        public FavoritesController(FavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddFavoriteRequestDTO request)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Unauthorized(new { error = "Invalid user token." });
            }

            try
            {
                BuildingDTO result = await _favoriteService.AddFavoriteAsync(userId, request.BuildingId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Unauthorized(new { error = "Invalid user token." });
            }

            try
            {
                List<BuildingDTO> favorites = await _favoriteService.GetFavoritesAsync(userId);
                return Ok(favorites);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{buildingId:int}")]
        public async Task<IActionResult> RemoveFavoriteAsync(int buildingId)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Unauthorized(new { error = "Invalid user token." });
            }

            try
            {
                await _favoriteService.RemoveFavoriteAsync(userId, buildingId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            string? userIdClaim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(userIdClaim, out userId);
        }
    }
}
