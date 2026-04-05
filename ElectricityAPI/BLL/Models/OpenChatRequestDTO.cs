namespace BLL.Models;

public class OpenChatRequestDTO
{
    public bool IsNewChat { get; set; }
    public int? ChatId { get; set; }
    public string Text { get; set; } = string.Empty;
}
