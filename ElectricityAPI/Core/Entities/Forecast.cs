using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Forecast
    {
        [Key]
        public int Id { get; set; }
        public double ConsumptionDay1 { get; set; }
        public double ConsumptionDay2 { get; set; }
        public double ConsumptionDay3 { get; set; }
        public DateTime CreatedAt { get; set; }
        public int BuildingId { get; set; }

        public Building Building { get; set; }
    }
}
