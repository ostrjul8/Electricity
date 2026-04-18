using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class WeatherRepository
    {
        private readonly AppDbContext _context;

        public WeatherRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<bool> HasAnyAsync()
        {
            return _context.WeatherRecords.AnyAsync();
        }

        public Task<List<WeatherRecord>> GetFromDateAsync(DateTime startDate)
        {
            DateTime startDateUtc = NormalizeToUtc(startDate);

            return _context.WeatherRecords
                .AsNoTracking()
                .Where(w => w.Date.Date >= startDateUtc.Date)
                .Select(w => new WeatherRecord
                {
                    Id = w.Id,
                    Date = w.Date.Date,
                    MinTemp = w.MinTemp,
                    MaxTemp = w.MaxTemp,
                    Condition = w.Condition,
                    WindSpeed = w.WindSpeed,
                    Humidity = w.Humidity
                })
                .ToListAsync();
        }

        public async Task<Dictionary<DateTime, WeatherRecord>> GetByStartDateAsync(DateTime startDate)
        {
            DateTime startDateUtc = NormalizeToUtc(startDate);

            List<WeatherRecord> records = await _context.WeatherRecords
                .Where(w => w.Date.Date >= startDateUtc.Date)
                .ToListAsync();

            return records
                .GroupBy(w => w.Date.Date)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First());
        }

        public async Task AddRangeAsync(IEnumerable<WeatherRecord> records)
        {
            await _context.WeatherRecords.AddRangeAsync(records);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public Task<int> DeleteOlderThanAsync(DateTime thresholdDate)
        {
            DateTime thresholdDateUtc = NormalizeToUtc(thresholdDate);

            return _context.WeatherRecords
                .Where(w => w.Date < thresholdDateUtc)
                .ExecuteDeleteAsync();
        }

        private static DateTime NormalizeToUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
