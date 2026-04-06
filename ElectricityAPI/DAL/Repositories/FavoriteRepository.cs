using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class FavoriteRepository
    {
        private readonly AppDbContext _context;

        public FavoriteRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<bool> ExistsAsync(int userId, int buildingId)
        {
            return _context.Favorites.AnyAsync(f => f.UserId == userId && f.BuildingId == buildingId);
        }

        public Task AddAsync(Favorite favorite)
        {
            return _context.Favorites.AddAsync(favorite).AsTask();
        }

        public Task<int> RemoveAsync(int userId, int buildingId)
        {
            return _context.Favorites
                .Where(f => f.UserId == userId && f.BuildingId == buildingId)
                .ExecuteDeleteAsync();
        }

        public Task<List<Favorite>> GetByUserIdWithBuildingAsync(int userId)
        {
            return _context.Favorites
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .Include(f => f.Building)
                    .ThenInclude(b => b.District)
                .OrderByDescending(f => f.Id)
                .ToListAsync();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
