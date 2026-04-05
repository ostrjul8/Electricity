using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public int UserId { get; set; }

        public User User { get; set; } = null!;
    }
}
