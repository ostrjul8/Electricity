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
        }
    }
}
