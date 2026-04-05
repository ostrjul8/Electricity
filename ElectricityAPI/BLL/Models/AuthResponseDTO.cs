namespace BLL.Models;

public class AuthResponseDTO
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string Role { get; set; } = string.Empty;
    public UserDTO User { get; set; } = new();
}
