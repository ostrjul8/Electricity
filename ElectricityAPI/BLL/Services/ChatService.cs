using BLL.Models;
using Core.Entities;
using DAL.Repositories;

namespace BLL.Services
{
    public class ChatService
    {
        private readonly ChatRepository _chatRepository;

        public ChatService(ChatRepository chatRepository)
        {
            _chatRepository = chatRepository;
        }

        public async Task<OpenChatResponseDTO> CreateChatWithFirstMessageAsync(int userId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Message text is required.");
            }

            Chat chat = new Chat
            {
                UserId = userId,
                IsRead = false
            };

            await _chatRepository.AddChatAsync(chat);
            await _chatRepository.SaveChangesAsync();

            Message message = new Message
            {
                ChatId = chat.Id,
                IsAdmin = false,
                Text = text.Trim(),
                SentAt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Kyiv")
            };

            await _chatRepository.AddMessageAsync(message);
            await _chatRepository.SaveChangesAsync();

            return new OpenChatResponseDTO
            {
                ChatId = chat.Id,
                IsNewChatCreated = true,
                Message = new MessageDTO
                {
                    Id = message.Id,
                    ChatId = message.ChatId,
                    IsAdmin = message.IsAdmin,
                    Text = message.Text,
                    SentAt = message.SentAt
                }
            };
        }

        public async Task<MessageDTO> SendMessageAsync(int userId, bool isAdmin, int chatId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Message text is required.");
            }

            Chat chat = isAdmin
                ? await _chatRepository.GetByIdAsync(chatId) ?? throw new KeyNotFoundException("Chat not found.")
                : await _chatRepository.GetByIdForUserAsync(chatId, userId) ?? throw new KeyNotFoundException("Chat not found.");

            chat.IsRead = false;

            Message message = new Message
            {
                ChatId = chat.Id,
                IsAdmin = isAdmin,
                Text = text.Trim(),
                SentAt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Kyiv")
            };

            await _chatRepository.AddMessageAsync(message);
            await _chatRepository.SaveChangesAsync();

            return new MessageDTO
            {
                Id = message.Id,
                ChatId = message.ChatId,
                IsAdmin = message.IsAdmin,
                Text = message.Text,
                SentAt = message.SentAt
            };
        }

        public async Task<List<ChatDTO>> GetChatsAsync(int userId, bool isAdmin)
        {
            List<Chat> chats = isAdmin
                ? await _chatRepository.GetAllAsync()
                : await _chatRepository.GetByUserIdAsync(userId);

            return chats.Select(c => new ChatDTO
            {
                Id = c.Id,
                UserId = c.UserId,
                IsRead = c.IsRead
            }).ToList();
        }

        public async Task<List<MessageDTO>> GetMessagesAsync(int userId, bool isAdmin, int chatId)
        {
            Chat chat = isAdmin
                ? await _chatRepository.GetByIdAsync(chatId) ?? throw new KeyNotFoundException("Chat not found.")
                : await _chatRepository.GetByIdForUserAsync(chatId, userId) ?? throw new KeyNotFoundException("Chat not found.");

            if (!chat.IsRead)
            {
                chat.IsRead = true;
                await _chatRepository.SaveChangesAsync();
            }

            List<Message> messages = await _chatRepository.GetMessagesByChatIdAsync(chat.Id);

            return messages.Select(m => new MessageDTO
            {
                Id = m.Id,
                ChatId = m.ChatId,
                IsAdmin = m.IsAdmin,
                Text = m.Text,
                SentAt = m.SentAt
            }).ToList();
        }
    }
}
