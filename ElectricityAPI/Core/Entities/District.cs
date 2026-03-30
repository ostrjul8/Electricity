using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class District
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<Building> Buildings { get; set; } = new List<Building>();
    }
}