using Microsoft.EntityFrameworkCore;
using Core.Entities;

namespace DAL.Repositories
{
    public class ForecastRepository
    {
        private readonly AppDbContext _context;

        public ForecastRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<int> DeleteOlderThanAsync(DateTime thresholdDate, CancellationToken cancellationToken = default)
        {
            return _context.Forecasts
                .Where(f => f.CreatedAt < thresholdDate)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public Task<Forecast?> GetLatestByBuildingIdAsync(int buildingId)
        {
            return _context.Forecasts
                .AsNoTracking()
                .Where(f => f.BuildingId == buildingId)
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
