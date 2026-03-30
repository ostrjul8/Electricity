namespace BLL.Models
{
    public class BuildingForecastDashboardDTO
    {
        public int BuildingId { get; set; }
        public List<DailyForecastDTO> CurrentWeekChart { get; set; } = new List<DailyForecastDTO>();
        public List<DailyForecastDTO> NextThreeDaysTable { get; set; } = new List<DailyForecastDTO>();
    }

    public class DailyForecastDTO
    {
        public DateTime Date { get; set; }
        public double ForecastedConsumption { get; set; }
    }
}
