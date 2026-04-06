using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class ChatRepository
    {
        private readonly AppDbContext _context;

        public ChatRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<Chat?> GetByIdAsync(int? userId, int chatId)
        {
            if (userId.HasValue)
            {
                return _context.Chats.FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId);
            }
            else
            {
                return _context.Chats.FirstOrDefaultAsync(c => c.Id == chatId);
            }
        }

        public Task<int> GetChatsCountAsync(int? userId, bool onlyUnread)
        {
            IQueryable<Chat> query = _context.Chats.AsNoTracking();

            if (userId.HasValue)
            {
                query = query.Where(c => c.UserId == userId);
            }

            if (onlyUnread)
            {
                query = query.Where(c => !c.IsRead);
            }

            return query.CountAsync();
        }

        public Task<List<Chat>> GetChatsPagedAsync(int? userId, bool onlyUnread, int skip, int take)
        {
            IQueryable<Chat> query = _context.Chats.AsNoTracking();

            if (userId.HasValue)
            {
                query = query.Where(c => c.UserId == userId);
            }

            if (onlyUnread)
            {
                query = query.Where(c => !c.IsRead);
            }

            return query
                .OrderByDescending(c => c.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public Task<int> GetMessagesCountByChatIdAsync(int chatId)
        {
            return _context.Messages
                .AsNoTracking()
                .Where(m => m.ChatId == chatId)
                .CountAsync();
        }

        public Task<List<Message>> GetMessagesPagedByChatIdAsync(int chatId, int skip, int take)
        {
            return _context.Messages
                .AsNoTracking()
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.SentAt)
                .ThenBy(m => m.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public Task AddChatAsync(Chat chat)
        {
            return _context.Chats.AddAsync(chat).AsTask();
        }

        public Task AddMessageAsync(Message message)
        {
            return _context.Messages.AddAsync(message).AsTask();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
