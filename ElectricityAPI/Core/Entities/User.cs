using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<Chat> Chats { get; set; } = new List<Chat>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }
}
