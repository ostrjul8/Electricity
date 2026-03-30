using System.Text.Json.Serialization;

namespace BLL.Models
{
    public class OsmBuildingJsonDTO
    {
        [JsonPropertyName("building")]
        public string BuildingType { get; set; }

        [JsonPropertyName("addr:street")]
        public string Street { get; set; }

        [JsonPropertyName("addr:housenumber")]
        public string HouseNumber { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("building:levels")]
        public string Levels { get; set; }

        [JsonPropertyName("material")]
        public string Material { get; set; }

        [JsonPropertyName("area_sqm")]
        public double Area { get; set; }

        [JsonPropertyName("coordinates")]
        public List<double> Coordinates { get; set; }
    }
}
