using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class BuildingRepository
    {
        private readonly AppDbContext _context;

        public BuildingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Building?> GetByIdWithDistrictAsync(int id)
        {
            return await _context.Buildings
                .AsNoTracking()
                .Include(b => b.District)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public Task<List<Building>> GetForConsumptionAsync()
        {
            return _context.Buildings
                .AsNoTracking()
                .Select(b => new Building
                {
                    Id = b.Id,
                    AverageConsumption = b.AverageConsumption
                })
                .ToListAsync();
        }

        public Task<List<Building>> GetForMapPointsAsync()
        {
            return _context.Buildings
                .AsNoTracking()
                .Where(b => b.Latitude != 0 && b.Longitude != 0)
                .Select(b => new Building
                {
                    Id = b.Id,
                    Latitude = b.Latitude,
                    Longitude = b.Longitude
                })
                .ToListAsync();
        }

        public Task<int> GetCountAsync()
        {
            return _context.Buildings.CountAsync();
        }

        public Task<List<Building>> GetByAddressAsync(string address, int take)
        {
            string normalizedQuery = address.Trim();

            return _context.Buildings
                .AsNoTracking()
                .Include(b => b.District)
                .Where(b => EF.Functions.ILike(b.Address, $"%{normalizedQuery}%") || EF.Functions.ILike(b.Name, $"%{normalizedQuery}%"))
                .OrderBy(b => b.Id)
                .Take(take)
                .ToListAsync();
        }

        public Task<List<Building>> GetPagedWithDistrictAsync(int skip, int take)
        {
            return _context.Buildings
                .AsNoTracking()
                .Include(b => b.District)
                .OrderBy(b => b.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
    }
}
