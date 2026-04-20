namespace BLL.Models;

public class OpenChatResponseDTO
{
    public int ChatId { get; set; }
    public bool IsNewChatCreated { get; set; }
    public string? GuestAccessToken { get; set; }
    public MessageDTO Message { get; set; } = new();
}
