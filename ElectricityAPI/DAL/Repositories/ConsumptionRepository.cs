using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class ConsumptionRepository
    {
        private readonly AppDbContext _context;

        public ConsumptionRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<bool> HasAnyAsync()
        {
            return _context.ConsumptionRecords.AnyAsync();
        }

        public Task<List<ConsumptionRecord>> GetLatestPerBuildingAsync()
        {
            return _context.ConsumptionRecords
                .AsNoTracking()
                .GroupBy(c => c.BuildingId)
                .Select(g => g
                    .OrderByDescending(c => c.Date)
                    .ThenByDescending(c => c.Id)
                    .First())
                .ToListAsync();
        }

        public Task<Dictionary<int, ConsumptionRecord>> GetByDateAsync(DateTime date)
        {
            return _context.ConsumptionRecords
                .Where(c => c.Date.Date == date.Date)
                .ToDictionaryAsync(c => c.BuildingId);
        }

        public Task AddRangeAsync(IEnumerable<ConsumptionRecord> records)
        {
            return _context.ConsumptionRecords.AddRangeAsync(records);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public void ClearChangeTracker()
        {
            _context.ChangeTracker.Clear();
        }

        public Task<int> DeleteOlderThanAsync(DateTime thresholdDate)
        {
            return _context.ConsumptionRecords
                .Where(c => c.Date < thresholdDate)
                .ExecuteDeleteAsync();
        }
    }
}
