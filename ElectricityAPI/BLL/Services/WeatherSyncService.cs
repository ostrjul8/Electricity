using BLL.Models;
using Core.Entities;
using DAL.Repositories;
using System.Globalization;
using System.Text.Json;

namespace BLL.Services
{
    public class WeatherSyncService
    {
        private readonly WeatherRepository _weatherRepository;
        private readonly HttpClient _httpClient;
        private const int DefaultForecastDays = 3;

        public WeatherSyncService(WeatherRepository weatherRepository, HttpClient httpClient)
        {
            _weatherRepository = weatherRepository;
            _httpClient = httpClient;
        }

        public async Task SyncWeatherAsync()
        {
            bool hasWeatherData = await _weatherRepository.HasAnyAsync();
            int pastDays = hasWeatherData ? 0 : 60;
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

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return;

            string jsonString = await response.Content.ReadAsStringAsync();
            OpenMeteoResponse? weatherData = JsonSerializer.Deserialize<OpenMeteoResponse>(jsonString);

            if (weatherData?.Daily == null) return;

            List<DateTime> parsedDates = weatherData.Daily.Time
                .Select(d => DateTime.ParseExact(
                    d,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal))
                .ToList();

            DateTime minDate = parsedDates.Min();

            Dictionary<DateTime, WeatherRecord> existingByDate = await _weatherRepository.GetByStartDateAsync(minDate);

            List<WeatherRecord> newRecords = new List<WeatherRecord>();
            bool hasUpdatedRecords = false;

            for (int i = 0; i < weatherData.Daily.Time.Count; i++)
            {
                DateTime date = parsedDates[i];

                existingByDate.TryGetValue(date.Date, out WeatherRecord? existingRecord);

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
                    await _weatherRepository.AddRangeAsync(newRecords);
                }

                await _weatherRepository.SaveChangesAsync();
            }
        }

        public async Task CleanupOldWeatherAsync()
        {
            DateTime now = KyivTimeHelper.Now;
            DateTime threeMonthsAgo = now.AddMonths(-3);

            await _weatherRepository.DeleteOlderThanAsync(threeMonthsAgo);
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
