using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Message
    {
        [Key]
        public int Id { get; set; }
        public int ChatId { get; set; }
        public bool IsAdmin { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }

        public Chat Chat { get; set; }
    }
}
