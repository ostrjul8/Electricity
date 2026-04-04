using DAL.Repositories;

namespace BLL.Services
{
    public class BuildingMapService
    {
        private readonly BuildingRepository _buildingRepository;

        public BuildingMapService(BuildingRepository buildingRepository)
        {
            _buildingRepository = buildingRepository;
        }

        public async Task<List<BuildingMapPointDTO>> GetMapPointsAsync()
        {
            var buildings = await _buildingRepository.GetForMapPointsAsync();

            if (!buildings.Any())
            {
                return new List<BuildingMapPointDTO>();
            }

            var minConsumption = buildings.Min(b => b.AverageConsumption);
            var maxConsumption = buildings.Max(b => b.AverageConsumption);

            return buildings.Select(b => new BuildingMapPointDTO
            {
                Id = b.Id,
                Longitude = b.Longitude,
                Latitude = b.Latitude,
                ColorLevel = CalculateColorLevel(b.AverageConsumption, minConsumption, maxConsumption)
            }).ToList();
        }

        private static int CalculateColorLevel(double value, double min, double max)
        {
            if (max <= min)
            {
                return 3;
            }

            var normalized = (value - min) / (max - min);
            var level = (int)Math.Ceiling(normalized * 5);

            return Math.Clamp(level, 1, 5);
        }
    }
}
