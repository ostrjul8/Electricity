namespace BLL.Models
{
    public class BuildingDetailsDTO
    {
        public BuildingDTO Building { get; set; } = new();
        public ForecastDTO LatestForecast { get; set; } = new();
    }
}
