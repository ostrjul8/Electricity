using BLL.Models;
using Core.Entities;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class FavoriteService
    {
        private readonly FavoriteRepository _favoriteRepository;
        private readonly BuildingRepository _buildingRepository;

        public FavoriteService(FavoriteRepository favoriteRepository, BuildingRepository buildingRepository)
        {
            _favoriteRepository = favoriteRepository;
            _buildingRepository = buildingRepository;
        }

        public async Task<BuildingDTO> AddFavoriteAsync(int userId, int buildingId)
        {
            Building? building = await _buildingRepository.GetByIdWithDistrictAsync(buildingId);

            if (building is null)
            {
                throw new KeyNotFoundException("Building not found.");
            }

            Favorite favorite = new Favorite
            {
                UserId = userId,
                BuildingId = buildingId
            };

            await _favoriteRepository.AddAsync(favorite);
            try
            {
                await _favoriteRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("Building is already in favorites.");
            }

            return MapBuilding(building);
        }

        public async Task<List<BuildingDTO>> GetFavoritesAsync(int userId)
        {
            List<Favorite> favorites = await _favoriteRepository.GetByUserIdWithBuildingAsync(userId);

            return favorites
                .Select(f => MapBuilding(f.Building))
                .ToList();
        }

        public async Task RemoveFavoriteAsync(int userId, int buildingId)
        {
            int affected = await _favoriteRepository.RemoveAsync(userId, buildingId);

            if (affected == 0)
            {
                throw new KeyNotFoundException("Favorite building not found.");
            }
        }

        private static BuildingDTO MapBuilding(Building building)
        {
            return new BuildingDTO
            {
                Id = building.Id,
                Type = building.Type,
                Address = building.Address,
                Name = building.Name,
                Floors = building.Floors,
                Material = building.Material,
                Area = building.Area,
                Longitude = building.Longitude,
                Latitude = building.Latitude,
                DistrictId = building.DistrictId,
                DistrictName = building.District?.Name ?? string.Empty,
                AverageConsumption = building.AverageConsumption
            };
        }
    }
}
