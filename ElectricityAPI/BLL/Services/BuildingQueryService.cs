using BLL.Models;
using Core.Entities;
using DAL.Repositories;
using System.Globalization;
using System.Text;

namespace BLL.Services
{
    public class BuildingQueryService
    {
        private readonly BuildingRepository _buildingRepository;
        private readonly ForecastRepository _forecastRepository;
        private readonly ConsumptionRepository _consumptionRepository;

        public BuildingQueryService(
            BuildingRepository buildingRepository,
            ForecastRepository forecastRepository,
            ConsumptionRepository consumptionRepository)
        {
            _buildingRepository = buildingRepository;
            _forecastRepository = forecastRepository;
            _consumptionRepository = consumptionRepository;
        }

        public async Task<(string FileName, string CsvContent)?> GenerateBuildingCsvReportAsync(int id)
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

            List<ConsumptionRecord> history = await _consumptionRepository.GetRecentByBuildingIdAsync(id, 60);
            List<ConsumptionRecord> orderedHistory = history.OrderBy(c => c.Date).ToList();

            StringBuilder csv = new StringBuilder();
            csv.AppendLine("BuildingId,BuildingName,Date,RecordType,ConsumptionAmount");

            foreach (ConsumptionRecord record in orderedHistory)
            {
                csv.AppendLine(string.Join(",",
                    buildingEntity.Id,
                    CsvEscape(buildingEntity.Name),
                    record.Date.ToString("yyyy-MM-dd"),
                    "actual",
                    record.ConsumptionAmount.ToString("0.####", CultureInfo.InvariantCulture)));
            }

            DateTime forecastBaseDate = latestForecastEntity.CreatedAt.Date;

            csv.AppendLine(string.Join(",",
                buildingEntity.Id,
                CsvEscape(buildingEntity.Name),
                forecastBaseDate.AddDays(1).ToString("yyyy-MM-dd"),
                "forecast_day_1",
                latestForecastEntity.ConsumptionDay1.ToString("0.####", CultureInfo.InvariantCulture)));

            csv.AppendLine(string.Join(",",
                buildingEntity.Id,
                CsvEscape(buildingEntity.Name),
                forecastBaseDate.AddDays(2).ToString("yyyy-MM-dd"),
                "forecast_day_2",
                latestForecastEntity.ConsumptionDay2.ToString("0.####", CultureInfo.InvariantCulture)));

            csv.AppendLine(string.Join(",",
                buildingEntity.Id,
                CsvEscape(buildingEntity.Name),
                forecastBaseDate.AddDays(3).ToString("yyyy-MM-dd"),
                "forecast_day_3",
                latestForecastEntity.ConsumptionDay3.ToString("0.####", CultureInfo.InvariantCulture)));

            string fileName = $"building-{buildingEntity.Id}-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            return (fileName, csv.ToString());
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

            List<ConsumptionPointDTO> recentConsumptions = (await _consumptionRepository.GetRecentByBuildingIdAsync(id, 4))
                .OrderBy(record => record.Date)
                .Select(record => new ConsumptionPointDTO
                {
                    Date = record.Date,
                    Amount = record.ConsumptionAmount
                })
                .ToList();

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
                LatestForecast = latestForecast,
                RecentConsumptions = recentConsumptions
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

        public async Task<List<BuildingDTO>> GetByAddressAsync(string address, int take = 5)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Address is required.");
            }

            List<Building> buildings = await _buildingRepository.GetByAddressAsync(address.Trim(), take);

            return buildings.Select(b => new BuildingDTO
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
        }

        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            string escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
    }
}
