using Core.Entities;
using DAL.Repositories;

namespace BLL.Services
{
    public class ConsumptionSyncService
    {
        private readonly ConsumptionRepository _consumptionRepository;
        private readonly BuildingRepository _buildingRepository;
        private readonly WeatherRepository _weatherRepository;

        public ConsumptionSyncService(
            ConsumptionRepository consumptionRepository,
            BuildingRepository buildingRepository,
            WeatherRepository weatherRepository)
        {
            _consumptionRepository = consumptionRepository;
            _buildingRepository = buildingRepository;
            _weatherRepository = weatherRepository;
        }

        public async Task SyncConsumptionAsync()
        {
            bool hasConsumptionData = await _consumptionRepository.HasAnyAsync();
            DateTime today = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Kyiv").Date;
            DateTime startDate = hasConsumptionData ? today : today.AddDays(-60);

            await SyncConsumptionWindowAsync(startDate, !hasConsumptionData);
        }

        public async Task EnsureConsumptionWindowAsync()
        {
            DateTime today = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Kyiv").Date;
            DateTime startDate = today.AddDays(-60);
            bool hasConsumptionData = await _consumptionRepository.HasAnyAsync();

            await SyncConsumptionWindowAsync(startDate, !hasConsumptionData);
        }

        private async Task SyncConsumptionWindowAsync(DateTime startDate, bool isInitialFill)
        {
            const int batchSize = 1000;

            List<Building> buildings = await _buildingRepository.GetForConsumptionAsync();

            if (!buildings.Any())
            {
                return;
            }

            List<WeatherRecord> weatherRecords = await _weatherRepository.GetFromDateAsync(startDate);

            if (!weatherRecords.Any())
            {
                return;
            }

            DateTime today = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Kyiv").Date;

            if (isInitialFill)
            {
                List<ConsumptionRecord> newRecords = new List<ConsumptionRecord>(batchSize);

                foreach (WeatherRecord weather in weatherRecords)
                {
                    foreach (Building building in buildings)
                    {
                        double hoursWithElectricity = GenerateHoursWithElectricity(weather.Condition);
                        double consumptionAmount = GenerateConsumptionAmount(
                            building.AverageConsumption,
                            hoursWithElectricity,
                            weather.MinTemp,
                            weather.MaxTemp);

                        newRecords.Add(new ConsumptionRecord
                        {
                            Date = weather.Date,
                            BuildingId = building.Id,
                            WeatherRecordId = weather.Id,
                            HoursWithElectricity = hoursWithElectricity,
                            ConsumptionAmount = consumptionAmount
                        });

                        if (newRecords.Count >= batchSize)
                        {
                            await _consumptionRepository.AddRangeAsync(newRecords);
                            await _consumptionRepository.SaveChangesAsync();
                            _consumptionRepository.ClearChangeTracker();
                            newRecords.Clear();
                        }
                    }
                }

                if (newRecords.Count > 0)
                {
                    await _consumptionRepository.AddRangeAsync(newRecords);
                    await _consumptionRepository.SaveChangesAsync();
                    _consumptionRepository.ClearChangeTracker();
                }

                return;
            }

            foreach (WeatherRecord weather in weatherRecords)
            {
                Dictionary<int, ConsumptionRecord> existingForDate = await _consumptionRepository.GetByDateAsync(weather.Date);

                List<ConsumptionRecord> newRecords = new List<ConsumptionRecord>();

                foreach (Building building in buildings)
                {
                    double hoursWithElectricity = GenerateHoursWithElectricity(weather.Condition);
                    double consumptionAmount = GenerateConsumptionAmount(
                        building.AverageConsumption,
                        hoursWithElectricity,
                        weather.MinTemp,
                        weather.MaxTemp);

                    if (existingForDate.TryGetValue(building.Id, out ConsumptionRecord? existingRecord))
                    {
                        if (weather.Date >= today)
                        {
                            existingRecord.HoursWithElectricity = hoursWithElectricity;
                            existingRecord.ConsumptionAmount = consumptionAmount;
                            existingRecord.WeatherRecordId = weather.Id;
                        }

                        continue;
                    }

                    newRecords.Add(new ConsumptionRecord
                    {
                        Date = weather.Date,
                        BuildingId = building.Id,
                        WeatherRecordId = weather.Id,
                        HoursWithElectricity = hoursWithElectricity,
                        ConsumptionAmount = consumptionAmount
                    });
                }

                if (newRecords.Any())
                {
                    await _consumptionRepository.AddRangeAsync(newRecords);
                }

                await _consumptionRepository.SaveChangesAsync();
                _consumptionRepository.ClearChangeTracker();
            }
        }

        public async Task CleanupOldConsumptionAsync()
        {
            DateTime now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Kyiv");
            DateTime threeMonthsAgo = now.AddMonths(-3);

            await _consumptionRepository.DeleteOlderThanAsync(threeMonthsAgo);
        }

        private static double GenerateHoursWithElectricity(string condition)
        {
            (double minHours, double maxHours) = condition switch
            {
                "Thunderstorm" => (4.0, 18.0),
                "Rain" or "Rain showers" or "Snow" or "Snow showers" => (5.0, 20.0),
                _ => (8.0, 24.0)
            };

            double hours = minHours + (Random.Shared.NextDouble() * (maxHours - minHours));
            return Math.Round(hours, 1);
        }

        private static double GenerateConsumptionAmount(double averageConsumption, double hoursWithElectricity, double minTemp, double maxTemp)
        {
            double baseForDay = Math.Max(averageConsumption, 0) * (hoursWithElectricity / 24.0);
            double variance = 1 + ((Random.Shared.NextDouble() * 0.30) - 0.15);
            double averageTemp = (minTemp + maxTemp) / 2.0;

            double weatherFactor = averageTemp switch
            {
                <= -5 or >= 30 => 1.15,
                <= 5 or >= 25 => 1.08,
                _ => 1.0
            };

            double value = baseForDay * variance * weatherFactor;
            return Math.Round(Math.Max(value, 0), 2);
        }
    }
}
