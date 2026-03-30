using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class WeatherRecord
    {
        [Key]
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public double MinTemp { get; set; }
        public double MaxTemp { get; set; }
        public string Condition { get; set; } = string.Empty;
        public double WindSpeed { get; set; }
        public int Humidity { get; set; }

        public ICollection<ConsumptionRecord> ConsumptionRecords { get; set; } = new List<ConsumptionRecord>();
    }
}