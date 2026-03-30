namespace BLL.Models
{
    public class BuildingDTO
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Floors { get; set; }
        public string Material { get; set; } = string.Empty;
        public double Area { get; set; }
        public int DistrictId { get; set; }
        public string DistrictName { get; set; } = string.Empty;
        public double AverageConsumption { get; set; }
    }
}
