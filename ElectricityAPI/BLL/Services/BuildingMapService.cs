using DAL.Repositories;

namespace BLL.Services
{
    public class BuildingMapService
    {
        private readonly BuildingRepository _buildingRepository;
        private readonly ConsumptionRepository _consumptionRepository;

        public BuildingMapService(BuildingRepository buildingRepository, ConsumptionRepository consumptionRepository)
        {
            _buildingRepository = buildingRepository;
            _consumptionRepository = consumptionRepository;
        }

        public async Task<List<BuildingMapPointDTO>> GetMapPointsAsync()
        {
            var buildings = await _buildingRepository.GetForMapPointsAsync();

            if (!buildings.Any())
            {
                return new List<BuildingMapPointDTO>();
            }

            var latestConsumptionRecords = await _consumptionRepository.GetLatestPerBuildingAsync();
            var latestByBuildingId = latestConsumptionRecords
                .ToDictionary(c => c.BuildingId, c => c.ConsumptionAmount);

            var mapValues = buildings
                .Where(b => latestByBuildingId.ContainsKey(b.Id))
                .Select(b => latestByBuildingId[b.Id])
                .ToList();

            var hasConsumptionValues = mapValues.Any();
            var p10 = hasConsumptionValues ? GetPercentile(mapValues, 0.10) : 0;
            var p90 = hasConsumptionValues ? GetPercentile(mapValues, 0.90) : 0;

            return buildings.Select(b => new BuildingMapPointDTO
            {
                Id = b.Id,
                Longitude = b.Longitude,
                Latitude = b.Latitude,
                ColorLevel = latestByBuildingId.TryGetValue(b.Id, out var latestConsumption)
                    ? CalculateColorLevel(latestConsumption, p10, p90)
                    : 3
            }).ToList();
        }

        private static int CalculateColorLevel(double value, double p10, double p90)
        {
            if (value < p10)
            {
                return 1;
            }

            if (value > p90)
            {
                return 6;
            }

            if (p90 <= p10)
            {
                return 3;
            }

            var normalized = (value - p10) / (p90 - p10);
            var bucket = (int)(normalized * 4);
            bucket = Math.Clamp(bucket, 0, 3);

            return 2 + bucket;
        }

        private static double GetPercentile(List<double> values, double percentile)
        {
            var ordered = values.OrderBy(v => v).ToList();

            if (ordered.Count == 1)
            {
                return ordered[0];
            }

            var position = (ordered.Count - 1) * percentile;
            var lowerIndex = (int)Math.Floor(position);
            var upperIndex = (int)Math.Ceiling(position);

            if (lowerIndex == upperIndex)
            {
                return ordered[lowerIndex];
            }

            var fraction = position - lowerIndex;
            return ordered[lowerIndex] + (ordered[upperIndex] - ordered[lowerIndex]) * fraction;
        }
    }
}
