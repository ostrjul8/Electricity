using BLL.Models;
using Core.Entities;
using DAL.Repositories;
using System.Collections.Concurrent;

namespace BLL.Services
{
    public class ChatService
    {
        private const string GuestUsername = "guest_support";
        private const string GuestEmail = "guest_support@local.invalid";
        private static readonly ConcurrentDictionary<int, string> GuestChatAccessTokens = new();

        private readonly ChatRepository _chatRepository;
        private readonly UserRepository _userRepository;

        public ChatService(ChatRepository chatRepository, UserRepository userRepository)
        {
            _chatRepository = chatRepository;
            _userRepository = userRepository;
        }

        public async Task<OpenChatResponseDTO> CreateChatWithFirstMessageAsync(int? userId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Message text is required.");
            }

            bool isGuestChat = !userId.HasValue;
            int resolvedUserId;

            if (userId.HasValue)
            {
                bool userExists = await _userRepository.ExistsByIdAsync(userId.Value);
                if (!userExists)
                {
                    throw new UnauthorizedAccessException("Invalid user token.");
                }

                resolvedUserId = userId.Value;
            }
            else
            {
                resolvedUserId = await EnsureGuestUserIdAsync();
            }

            Chat chat = new Chat
            {
                UserId = resolvedUserId,
                IsRead = false
            };

            await _chatRepository.AddChatAsync(chat);
            await _chatRepository.SaveChangesAsync();

            Message message = new Message
            {
                ChatId = chat.Id,
                IsAdmin = false,
                Text = text.Trim(),
                SentAt = DateTime.UtcNow
            };

            await _chatRepository.AddMessageAsync(message);
            await _chatRepository.SaveChangesAsync();

            string? guestAccessToken = null;
            if (isGuestChat)
            {
                guestAccessToken = RegisterGuestAccessToken(chat.Id);
            }

            return new OpenChatResponseDTO
            {
                ChatId = chat.Id,
                IsNewChatCreated = true,
                GuestAccessToken = guestAccessToken,
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

        private async Task<int> EnsureGuestUserIdAsync()
        {
            User? existingByUsername = await _userRepository.GetByUsernameAsync(GuestUsername);
            if (existingByUsername is not null)
            {
                return existingByUsername.Id;
            }

            User? existingByEmail = await _userRepository.GetByEmailAsync(GuestEmail);
            if (existingByEmail is not null)
            {
                return existingByEmail.Id;
            }

            User guestUser = new User
            {
                Username = GuestUsername,
                Email = GuestEmail,
                PasswordHash = "GUEST_ACCOUNT_NO_LOGIN",
                IsAdmin = false,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(guestUser);
            await _userRepository.SaveChangesAsync();

            return guestUser.Id;
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
                SentAt = DateTime.UtcNow
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

        public async Task<MessageDTO> SendGuestMessageAsync(int chatId, string accessToken, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Message text is required.");
            }

            Chat chat = await GetGuestChatByAccessTokenAsync(chatId, accessToken);

            chat.IsRead = false;

            Message message = new Message
            {
                ChatId = chat.Id,
                IsAdmin = false,
                Text = text.Trim(),
                SentAt = DateTime.UtcNow,
            };

            await _chatRepository.AddMessageAsync(message);
            await _chatRepository.SaveChangesAsync();

            return new MessageDTO
            {
                Id = message.Id,
                ChatId = message.ChatId,
                IsAdmin = message.IsAdmin,
                Text = message.Text,
                SentAt = message.SentAt,
            };
        }

        public async Task<PagedResultDTO<MessageDTO>> GetGuestMessagesPagedAsync(
            int chatId,
            string accessToken,
            int page,
            int pageSize)
        {
            Chat chat = await GetGuestChatByAccessTokenAsync(chatId, accessToken);

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
                    SentAt = m.SentAt,
                }).ToList(),
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

            if (isAdmin && !chat.IsRead)
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

        private string RegisterGuestAccessToken(int chatId)
        {
            string token = Guid.NewGuid().ToString("N");
            GuestChatAccessTokens.AddOrUpdate(chatId, token, (_, _) => token);

            return token;
        }

        private async Task<Chat> GetGuestChatByAccessTokenAsync(int chatId, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new UnauthorizedAccessException("Guest access token is required.");
            }

            bool hasMatchingToken = GuestChatAccessTokens.TryGetValue(chatId, out string? expectedAccessToken)
                && string.Equals(expectedAccessToken, accessToken, StringComparison.Ordinal);

            if (!hasMatchingToken)
            {
                throw new UnauthorizedAccessException("Invalid guest access token.");
            }

            Chat? chat = await _chatRepository.GetByIdAsync(null, chatId);
            if (chat == null)
            {
                throw new KeyNotFoundException("Chat not found.");
            }

            User? chatOwner = await _userRepository.GetByIdAsync(chat.UserId);
            bool isGuestChat = chatOwner is not null
                && string.Equals(chatOwner.Username, GuestUsername, StringComparison.Ordinal);

            if (!isGuestChat)
            {
                throw new UnauthorizedAccessException("Chat is not available for guest access.");
            }

            return chat;
        }
    }
}
