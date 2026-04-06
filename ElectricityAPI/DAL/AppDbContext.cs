using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Forecast> Forecasts { get; set; }
        public DbSet<WeatherRecord> WeatherRecords { get; set; }
        public DbSet<ConsumptionRecord> ConsumptionRecords { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Favorite> Favorites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ConsumptionRecord>()
                .HasOne(c => c.Building)
                .WithMany(b => b.ConsumptionRecords)
                .HasForeignKey(c => c.BuildingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConsumptionRecord>()
                .HasOne(c => c.WeatherRecord)
                .WithMany(w => w.ConsumptionRecords)
                .HasForeignKey(c => c.WeatherRecordId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.TokenHash)
                .IsUnique();

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Building)
                .WithMany(b => b.Favorites)
                .HasForeignKey(f => f.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Favorite>()
                .HasIndex(f => new { f.UserId, f.BuildingId })
                .IsUnique();
        }
    }
}
