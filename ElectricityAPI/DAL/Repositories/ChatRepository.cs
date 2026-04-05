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

        public Task<Chat?> GetByIdForUserAsync(int chatId, int userId)
        {
            return _context.Chats
                .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId);
        }

        public Task<Chat?> GetByIdAsync(int chatId)
        {
            return _context.Chats.FirstOrDefaultAsync(c => c.Id == chatId);
        }

        public Task<List<Chat>> GetByUserIdAsync(int userId)
        {
            return _context.Chats
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.Id)
                .ToListAsync();
        }

        public Task<List<Chat>> GetAllAsync()
        {
            return _context.Chats
                .AsNoTracking()
                .OrderByDescending(c => c.Id)
                .ToListAsync();
        }

        public Task<List<Message>> GetMessagesByChatIdAsync(int chatId)
        {
            return _context.Messages
                .AsNoTracking()
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.SentAt)
                .ThenBy(m => m.Id)
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
