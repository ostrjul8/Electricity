using BLL.Models;
using Core.Entities;
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
            Building? buildingEntity = await _buildingRepository.GetByIdWithDistrictAsync(id);

            if (buildingEntity is null)
            {
                return null;
            }

            Forecast? latestForecastEntity = await _forecastRepository.GetLatestByBuildingIdAsync(id);

            if (latestForecastEntity is null)
            {
                return null;
            }

            BuildingDTO building = new BuildingDTO
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

            ForecastDTO latestForecast = new ForecastDTO
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

        public async Task<PagedResultDTO<BuildingDTO>> GetPagedBuildingsAsync(int page, int pageSize)
        {
            int totalCount = await _buildingRepository.GetCountAsync();
            int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);
            int skip = (page - 1) * pageSize;

            List<Building> buildings = await _buildingRepository.GetPagedWithDistrictAsync(skip, pageSize);

            List<BuildingDTO> items = buildings.Select(b => new BuildingDTO
            {
                Id = b.Id,
                Type = b.Type,
                Address = b.Address,
                Name = b.Name,
                Floors = b.Floors,
                Material = b.Material,
                Area = b.Area,
                Longitude = b.Longitude,
                Latitude = b.Latitude,
                DistrictId = b.DistrictId,
                DistrictName = b.District?.Name ?? string.Empty,
                AverageConsumption = b.AverageConsumption
            }).ToList();

            return new PagedResultDTO<BuildingDTO>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Items = items
            };
        }

        public async Task<BuildingDTO?> GetByAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Address is required.");
            }

            Building? building = await _buildingRepository.GetByAddressAsync(address.Trim());

            if (building is null)
            {
                return null;
            }

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
