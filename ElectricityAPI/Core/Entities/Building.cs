using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Building
    {
        [Key]
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Floors { get; set; }
        public string Material { get; set; } = string.Empty;
        public double Area { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public int DistrictId { get; set; }
        public double AverageConsumption { get; set; }

        public District? District { get; set; }
        public ICollection<Forecast> Forecasts { get; set; } = new List<Forecast>();
        public ICollection<ConsumptionRecord> ConsumptionRecords { get; set; } = new List<ConsumptionRecord>();
    }
}
