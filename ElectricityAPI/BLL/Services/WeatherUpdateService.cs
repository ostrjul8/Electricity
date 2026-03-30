using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class WeatherUpdateService : BackgroundService
    {
        private readonly ILogger<WeatherUpdateService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public WeatherUpdateService(ILogger<WeatherUpdateService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Weather update background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Starting weather data update for districts...");

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var weatherSyncService = scope.ServiceProvider.GetRequiredService<WeatherSyncService>();

                        await weatherSyncService.SyncWeatherAsync();
                        await weatherSyncService.CleanupOldWeatherAsync();
                    }

                    _logger.LogInformation("Weather data updated successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while updating weather data.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
