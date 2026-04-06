using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Favorite
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BuildingId { get; set; }

        public User User { get; set; } = null!;
        public Building Building { get; set; } = null!;
    }
}
