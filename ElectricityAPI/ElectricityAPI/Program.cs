using BLL.Services;
using DAL;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ElectricityAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddHostedService<WeatherUpdateService>();
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<WeatherSyncService>();
            builder.Services.AddScoped<ConsumptionSyncService>();
            builder.Services.AddScoped<ForecastScriptService>();
            builder.Services.AddScoped<BuildingRepository>();
            builder.Services.AddScoped<WeatherRepository>();
            builder.Services.AddScoped<ConsumptionRepository>();
            builder.Services.AddScoped<ForecastRepository>();
            builder.Services.AddScoped<BuildingQueryService>();
            builder.Services.AddScoped<BuildingMapService>();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<AppDbContext>();

                await ElectricityAPI.Data.DatabaseSeeder.SeedBuildingsAsync(context);
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
