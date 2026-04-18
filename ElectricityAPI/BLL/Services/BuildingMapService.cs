using Core.Entities;
using DAL.Repositories;

namespace BLL.Services
{
    public class BuildingMapService
    {
        private const int AnomalyColorLevel = 7;

        private readonly BuildingRepository _buildingRepository;
        private readonly ConsumptionRepository _consumptionRepository;

        public BuildingMapService(BuildingRepository buildingRepository, ConsumptionRepository consumptionRepository)
        {
            _buildingRepository = buildingRepository;
            _consumptionRepository = consumptionRepository;
        }

        public async Task<List<BuildingMapPointDTO>> GetMapPointsAsync()
        {
            List<Building> buildings = await _buildingRepository.GetForMapPointsAsync();

            if (!buildings.Any())
            {
                return new List<BuildingMapPointDTO>();
            }

            List<ConsumptionRecord> latestConsumptionRecords = await _consumptionRepository.GetLatestPerBuildingAsync();
            Dictionary<int, double> latestByBuildingId = latestConsumptionRecords
                .ToDictionary(c => c.BuildingId, c => c.ConsumptionAmount);

            List<double> mapValues = buildings
                .Where(b => latestByBuildingId.ContainsKey(b.Id))
                .Select(b => latestByBuildingId[b.Id])
                .ToList();

            bool hasConsumptionValues = mapValues.Any();
            double p10 = hasConsumptionValues ? GetPercentile(mapValues, 0.05) : 0;
            double p90 = hasConsumptionValues ? GetPercentile(mapValues, 0.95) : 0;

            return buildings.Select(b => new BuildingMapPointDTO
            {
                Id = b.Id,
                Longitude = b.Longitude,
                Latitude = b.Latitude,
                ColorLevel = latestByBuildingId.TryGetValue(b.Id, out double latestConsumption)
                    ? CalculateColorLevel(latestConsumption, p10, p90)
                    : 3
            }).ToList();
        }

        public async Task<List<BuildingMapPointDTO>> GetAnomalyMapPointsAsync(double deviationPercent)
        {
            List<Building> buildings = await _buildingRepository.GetForMapPointsAsync();
            if (!buildings.Any())
            {
                return new List<BuildingMapPointDTO>();
            }

            List<ConsumptionRecord> latestConsumptionRecords = await _consumptionRepository.GetLatestPerBuildingAsync();
            Dictionary<int, double> latestByBuildingId = latestConsumptionRecords
                .ToDictionary(c => c.BuildingId, c => c.ConsumptionAmount);

            List<(Building Building, double LatestConsumption)> anomalies = buildings
                .Where(b => latestByBuildingId.TryGetValue(b.Id, out _))
                .Select(b => (Building: b, LatestConsumption: latestByBuildingId[b.Id]))
                .Where(entry => GetPositiveDeviationPercent(entry.Building.AverageConsumption, entry.LatestConsumption) >= deviationPercent)
                .ToList();

            if (!anomalies.Any())
            {
                return new List<BuildingMapPointDTO>();
            }

            return anomalies.Select(a => new BuildingMapPointDTO
            {
                Id = a.Building.Id,
                Longitude = a.Building.Longitude,
                Latitude = a.Building.Latitude,
                ColorLevel = AnomalyColorLevel
            }).ToList();
        }

        private static double GetPositiveDeviationPercent(double normalValue, double currentValue)
        {
            if (normalValue <= 0)
            {
                return currentValue > normalValue ? double.PositiveInfinity : 0;
            }

            return currentValue <= normalValue ? 0 : (currentValue - normalValue) / normalValue * 100;
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

            double normalized = (value - p10) / (p90 - p10);
            int bucket = (int)(normalized * 4);
            bucket = Math.Clamp(bucket, 0, 3);

            return 2 + bucket;
        }

        private static double GetPercentile(List<double> values, double percentile)
        {
            List<double> ordered = values.OrderBy(v => v).ToList();

            if (ordered.Count == 1)
            {
                return ordered[0];
            }

            double position = (ordered.Count - 1) * percentile;
            int lowerIndex = (int)Math.Floor(position);
            int upperIndex = (int)Math.Ceiling(position);

            if (lowerIndex == upperIndex)
            {
                return ordered[lowerIndex];
            }

            double fraction = position - lowerIndex;
            return ordered[lowerIndex] + (ordered[upperIndex] - ordered[lowerIndex]) * fraction;
        }
    }
}
