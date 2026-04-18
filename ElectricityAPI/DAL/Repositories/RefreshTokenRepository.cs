using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class RefreshTokenRepository
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task AddAsync(RefreshToken refreshToken)
        {
            return _context.RefreshTokens.AddAsync(refreshToken).AsTask();
        }

        public Task<RefreshToken?> GetActiveByHashAsync(string tokenHash)
        {
            return _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.RevokedAt == null);
        }

        public Task<RefreshToken?> GetByHashAsync(string tokenHash)
        {
            return _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        }

        public Task<int> TryRevokeAsync(int refreshTokenId, DateTime revokedAt)
        {
            DateTime revokedAtUtc = NormalizeToUtc(revokedAt);

            return _context.RefreshTokens
                .Where(rt => rt.Id == refreshTokenId && rt.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(rt => rt.RevokedAt, revokedAtUtc));
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public Task<int> DeleteExpiredRevokedAsync(DateTime olderThan)
        {
            DateTime olderThanUtc = NormalizeToUtc(olderThan);

            return _context.RefreshTokens
                .Where(rt => rt.RevokedAt != null && rt.RevokedAt < olderThanUtc)
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
