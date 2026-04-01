using Core.Entities;
using DAL;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class ConsumptionSyncService
    {
        private readonly AppDbContext _context;

        public ConsumptionSyncService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SyncConsumptionAsync()
        {
            var hasConsumptionData = await _context.ConsumptionRecords.AnyAsync();
            var startDate = hasConsumptionData ? DateTime.UtcNow.Date : DateTime.UtcNow.Date.AddDays(-60);

            await SyncConsumptionWindowAsync(startDate, !hasConsumptionData);
        }

        public async Task EnsureConsumptionWindowAsync()
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-60);
            var hasConsumptionData = await _context.ConsumptionRecords.AnyAsync();

            await SyncConsumptionWindowAsync(startDate, !hasConsumptionData);
        }

        private async Task SyncConsumptionWindowAsync(DateTime startDate, bool isInitialFill)
        {
            const int batchSize = 1000;

            var buildings = await _context.Buildings
                .AsNoTracking()
                .Select(b => new { b.Id, b.AverageConsumption })
                .ToListAsync();

            if (!buildings.Any())
            {
                return;
            }

            var weatherRecords = await _context.WeatherRecords
                .AsNoTracking()
                .Where(w => w.Date.Date >= startDate)
                .Select(w => new
                {
                    w.Id,
                    Date = w.Date.Date,
                    w.MinTemp,
                    w.MaxTemp,
                    w.Condition
                })
                .ToListAsync();

            if (!weatherRecords.Any())
            {
                return;
            }

            var today = DateTime.UtcNow.Date;

            if (isInitialFill)
            {
                var newRecords = new List<ConsumptionRecord>(batchSize);

                foreach (var weather in weatherRecords)
                {
                    foreach (var building in buildings)
                    {
                        var hoursWithElectricity = GenerateHoursWithElectricity(weather.Condition);
                        var consumptionAmount = GenerateConsumptionAmount(
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
                            _context.ConsumptionRecords.AddRange(newRecords);
                            await _context.SaveChangesAsync();
                            _context.ChangeTracker.Clear();
                            newRecords.Clear();
                        }
                    }
                }

                if (newRecords.Count > 0)
                {
                    _context.ConsumptionRecords.AddRange(newRecords);
                    await _context.SaveChangesAsync();
                    _context.ChangeTracker.Clear();
                }

                return;
            }

            foreach (var weather in weatherRecords)
            {
                var existingForDate = await _context.ConsumptionRecords
                    .Where(c => c.Date.Date == weather.Date)
                    .ToDictionaryAsync(c => c.BuildingId);

                var newRecords = new List<ConsumptionRecord>();

                foreach (var building in buildings)
                {
                    var hoursWithElectricity = GenerateHoursWithElectricity(weather.Condition);
                    var consumptionAmount = GenerateConsumptionAmount(
                        building.AverageConsumption,
                        hoursWithElectricity,
                        weather.MinTemp,
                        weather.MaxTemp);

                    if (existingForDate.TryGetValue(building.Id, out var existingRecord))
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
                    _context.ConsumptionRecords.AddRange(newRecords);
                }

                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();
            }
        }

        public async Task CleanupOldConsumptionAsync()
        {
            var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);

            await _context.ConsumptionRecords
                .Where(c => c.Date < threeMonthsAgo)
                .ExecuteDeleteAsync();
        }

        private static double GenerateHoursWithElectricity(string condition)
        {
            var (minHours, maxHours) = condition switch
            {
                "Thunderstorm" => (4.0, 18.0),
                "Rain" or "Rain showers" or "Snow" or "Snow showers" => (5.0, 20.0),
                _ => (8.0, 24.0)
            };

            var hours = minHours + (Random.Shared.NextDouble() * (maxHours - minHours));
            return Math.Round(hours, 1);
        }

        private static double GenerateConsumptionAmount(double averageConsumption, double hoursWithElectricity, double minTemp, double maxTemp)
        {
            var baseForDay = Math.Max(averageConsumption, 0) * (hoursWithElectricity / 24.0);
            var variance = 1 + ((Random.Shared.NextDouble() * 0.30) - 0.15);
            var averageTemp = (minTemp + maxTemp) / 2.0;

            var weatherFactor = averageTemp switch
            {
                <= -5 or >= 30 => 1.15,
                <= 5 or >= 25 => 1.08,
                _ => 1.0
            };

            var value = baseForDay * variance * weatherFactor;
            return Math.Round(Math.Max(value, 0), 2);
        }
    }
}
