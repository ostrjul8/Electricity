using BLL.Models;
using BLL.Models;
using Core.Entities;
using DAL;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BLL.Services
{
    public class WeatherSyncService
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private const int DefaultForecastDays = 3;

        public WeatherSyncService(AppDbContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        public async Task SyncWeatherAsync()
        {
            var hasWeatherData = await _context.WeatherRecords.AnyAsync();
            var pastDays = hasWeatherData ? 0 : 60;
            await SyncWeatherWindowAsync(pastDays, DefaultForecastDays);
        }

        public async Task EnsureWeatherWindowAsync()
        {
            const int pastDaysIncludingToday = 61;
            await SyncWeatherWindowAsync(pastDaysIncludingToday, DefaultForecastDays);
        }

        private async Task SyncWeatherWindowAsync(int pastDays, int forecastDays)
        {
            string url = $"https://api.open-meteo.com/v1/forecast?latitude=50.45&longitude=30.52&daily=weather_code,temperature_2m_max,temperature_2m_min,wind_speed_10m_max,relative_humidity_2m_mean&forecast_days={forecastDays}&past_days={pastDays}&timezone=Europe%2FKyiv";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return;

            var jsonString = await response.Content.ReadAsStringAsync();
            var weatherData = JsonSerializer.Deserialize<OpenMeteoResponse>(jsonString);

            if (weatherData?.Daily == null) return;

            var newRecords = new List<WeatherRecord>();
            var hasUpdatedRecords = false;

            for (int i = 0; i < weatherData.Daily.Time.Count; i++)
            {
                var date = DateTime.Parse(weatherData.Daily.Time[i]).ToUniversalTime();

                var existingRecord = await _context.WeatherRecords
                    .FirstOrDefaultAsync(w => w.Date.Date == date.Date);

                if (existingRecord is not null)
                {
                    existingRecord.MaxTemp = weatherData.Daily.MaxTemp[i];
                    existingRecord.MinTemp = weatherData.Daily.MinTemp[i];
                    existingRecord.WindSpeed = weatherData.Daily.WindSpeed[i];
                    existingRecord.Humidity = (int)weatherData.Daily.Humidity[i];
                    existingRecord.Condition = GetConditionFromCode(weatherData.Daily.WeatherCode[i]);
                    hasUpdatedRecords = true;
                }
                else
                {
                    newRecords.Add(new WeatherRecord
                    {
                        Date = date,
                        MaxTemp = weatherData.Daily.MaxTemp[i],
                        MinTemp = weatherData.Daily.MinTemp[i],
                        WindSpeed = weatherData.Daily.WindSpeed[i],
                        Humidity = (int)weatherData.Daily.Humidity[i],
                        Condition = GetConditionFromCode(weatherData.Daily.WeatherCode[i])
                    });
                }
            }

            if (newRecords.Any() || hasUpdatedRecords)
            {
                if (newRecords.Any())
                {
                    _context.WeatherRecords.AddRange(newRecords);
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task CleanupOldWeatherAsync()
        {
            var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);

            await _context.WeatherRecords
                .Where(w => w.Date < threeMonthsAgo)
                .ExecuteDeleteAsync();
        }

        private string GetConditionFromCode(int code)
        {
            return code switch
            {
                0 => "Clear sky",
                1 or 2 or 3 => "Cloudy",
                45 or 48 => "Fog",
                51 or 53 or 55 or 56 or 57 => "Drizzle",
                61 or 63 or 65 or 66 or 67 => "Rain",
                71 or 73 or 75 or 77 => "Snow",
                80 or 81 or 82 => "Rain showers",
                85 or 86 => "Snow showers",
                95 or 96 or 99 => "Thunderstorm",
                _ => "Unknown"
            };
        }
    }
}
