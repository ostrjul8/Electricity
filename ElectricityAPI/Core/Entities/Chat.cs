using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Chat
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool IsRead { get; set; }

        public User User { get; set; }
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
