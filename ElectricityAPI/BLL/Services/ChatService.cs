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
                SentAt = KyivTimeHelper.Now
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

            int? filteredUserId = isAdmin ? null : userId;
            
            Chat? chat = await _chatRepository.GetByIdAsync(filteredUserId, chatId);

            if (chat == null)
            {
                throw new KeyNotFoundException("Chat not found.");
            }

            chat.IsRead = false;

            Message message = new Message
            {
                ChatId = chat.Id,
                IsAdmin = isAdmin,
                Text = text.Trim(),
                SentAt = KyivTimeHelper.Now
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
        public async Task<PagedResultDTO<ChatDTO>> GetChatsPagedAsync(int userId, bool isAdmin, bool onlyUnread, int page, int pageSize)
        {
            int? filteredUserId = isAdmin ? null : userId;
            int totalCount = await _chatRepository.GetChatsCountAsync(filteredUserId, onlyUnread);
            int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);
            int skip = (page - 1) * pageSize;

            List<Chat> chats = await _chatRepository.GetChatsPagedAsync(filteredUserId, onlyUnread, skip, pageSize);

            return new PagedResultDTO<ChatDTO>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Items = chats.Select(c => new ChatDTO
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    IsRead = c.IsRead
                }).ToList()
            };
        }

        public async Task<PagedResultDTO<MessageDTO>> GetMessagesPagedAsync(int userId, bool isAdmin, int chatId, int page, int pageSize)
        {
            int? filteredUserId = isAdmin ? null : userId;

            Chat? chat = await _chatRepository.GetByIdAsync(filteredUserId, chatId);

            if (chat == null)
            {
                throw new KeyNotFoundException("Chat not found.");
            }

            if (!chat.IsRead)
            {
                chat.IsRead = true;
                await _chatRepository.SaveChangesAsync();
            }

            int totalCount = await _chatRepository.GetMessagesCountByChatIdAsync(chat.Id);
            int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);
            int skip = (page - 1) * pageSize;

            List<Message> messages = await _chatRepository.GetMessagesPagedByChatIdAsync(chat.Id, skip, pageSize);

            return new PagedResultDTO<MessageDTO>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Items = messages.Select(m => new MessageDTO
                {
                    Id = m.Id,
                    ChatId = m.ChatId,
                    IsAdmin = m.IsAdmin,
                    Text = m.Text,
                    SentAt = m.SentAt
                }).ToList()
            };
        }
    }
}
