using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class UserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<bool> ExistsByEmailAsync(string email)
        {
            return _context.Users.AnyAsync(u => u.Email == email);
        }

        public Task<bool> ExistsByUsernameAsync(string username)
        {
            return _context.Users.AnyAsync(u => u.Username.ToLowerInvariant() == username.ToLowerInvariant());
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            return _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public Task<User?> GetByUsernameAsync(string username)
        {
            return _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public Task<User?> GetByIdAsync(int id)
        {
            return _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public Task<bool> ExistsByIdAsync(int id)
        {
            return _context.Users.AnyAsync(u => u.Id == id);
        }

        public Task<List<User>> GetAllAsync()
        {
            return _context.Users
                .AsNoTracking()
                .OrderBy(u => u.Id)
                .ToListAsync();
        }

        public Task AddAsync(User user)
        {
            return _context.Users.AddAsync(user).AsTask();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
