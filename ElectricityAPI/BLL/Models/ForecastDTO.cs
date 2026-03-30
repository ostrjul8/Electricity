namespace BLL.Models
{
    public class ForecastDTO
    {
        public int Id { get; set; }
        public double ConsumptionDay1 { get; set; }
        public double ConsumptionDay2 { get; set; }
        public double ConsumptionDay3 { get; set; }
        public DateTime CreatedAt { get; set; }
        public int BuildingId { get; set; }
    }
}
