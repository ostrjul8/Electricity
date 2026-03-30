using System.Text.Json.Serialization;

namespace BLL.Models
{
    public class OpenMeteoResponse
    {
        [JsonPropertyName("daily")]
        public required OpenMeteoDaily Daily { get; set; }
    }

    public class OpenMeteoDaily
    {
        [JsonPropertyName("time")]
        public required List<string> Time { get; set; }

        [JsonPropertyName("temperature_2m_max")]
        public required List<double> MaxTemp { get; set; }

        [JsonPropertyName("temperature_2m_min")]
        public required List<double> MinTemp { get; set; }

        [JsonPropertyName("weather_code")]
        public required List<int> WeatherCode { get; set; }

        [JsonPropertyName("wind_speed_10m_max")]
        public required List<double> WindSpeed { get; set; }

        [JsonPropertyName("relative_humidity_2m_mean")]
        public required List<double> Humidity { get; set; }
    }
}
