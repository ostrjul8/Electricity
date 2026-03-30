using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class ConsumptionRecord
    {
        [Key]
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public double ConsumptionAmount { get; set; }
        public double HoursWithElectricity { get; set; }
        public int BuildingId { get; set; }
        public int WeatherRecordId { get; set; }

        public Building Building { get; set; }
        public WeatherRecord WeatherRecord { get; set; }
    }
}