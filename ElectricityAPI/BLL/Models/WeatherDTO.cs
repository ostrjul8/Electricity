namespace BLL.Models
{
    public class WeatherDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public double MinTemp { get; set; }
        public double MaxTemp { get; set; }
        public string Condition { get; set; } = string.Empty;
        public double WindSpeed { get; set; }
        public int Humidity { get; set; }
    }
}
