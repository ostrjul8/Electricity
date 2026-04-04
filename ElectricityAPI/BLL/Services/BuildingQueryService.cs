using BLL.Models;
using DAL.Repositories;

namespace BLL.Services
{
    public class BuildingQueryService
    {
        private readonly BuildingRepository _buildingRepository;
        private readonly ForecastRepository _forecastRepository;

        public BuildingQueryService(BuildingRepository buildingRepository, ForecastRepository forecastRepository)
        {
            _buildingRepository = buildingRepository;
            _forecastRepository = forecastRepository;
        }

        public async Task<BuildingDetailsDTO?> GetBuildingDetailsAsync(int id)
        {
            var buildingEntity = await _buildingRepository.GetByIdWithDistrictAsync(id);

            if (buildingEntity is null)
            {
                return null;
            }

            var latestForecastEntity = await _forecastRepository.GetLatestByBuildingIdAsync(id);

            var building = new BuildingDTO
            {
                Id = buildingEntity.Id,
                Type = buildingEntity.Type,
                Address = buildingEntity.Address,
                Name = buildingEntity.Name,
                Floors = buildingEntity.Floors,
                Material = buildingEntity.Material,
                Area = buildingEntity.Area,
                Longitude = buildingEntity.Longitude,
                Latitude = buildingEntity.Latitude,
                DistrictId = buildingEntity.DistrictId,
                DistrictName = buildingEntity.District?.Name ?? string.Empty,
                AverageConsumption = buildingEntity.AverageConsumption
            };

            ForecastDTO? latestForecast = latestForecastEntity is null
                ? null
                : new ForecastDTO
                {
                    Id = latestForecastEntity.Id,
                    BuildingId = latestForecastEntity.BuildingId,
                    ConsumptionDay1 = latestForecastEntity.ConsumptionDay1,
                    ConsumptionDay2 = latestForecastEntity.ConsumptionDay2,
                    ConsumptionDay3 = latestForecastEntity.ConsumptionDay3,
                    CreatedAt = latestForecastEntity.CreatedAt
                };

            return new BuildingDetailsDTO
            {
                Building = building,
                LatestForecast = latestForecast
            };
        }
    }
}
