using DAL;
using DAL.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BLL.Services
{
    public class ForecastScriptService
    {
        private readonly ForecastRepository _forecastRepository;
        private readonly ILogger<ForecastScriptService> _logger;
        private readonly IConfiguration _configuration;

        public ForecastScriptService(ForecastRepository forecastRepository, ILogger<ForecastScriptService> logger, IConfiguration configuration)
        {
            _forecastRepository = forecastRepository;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task RunForecastScriptAsync(CancellationToken cancellationToken = default)
        {
            var pythonExecutable = _configuration["ForecastScript:PythonExecutable"] ?? "python";
            var configuredPath = _configuration["ForecastScript:ScriptPath"];
            var scriptPath = ResolveScriptPath(configuredPath);

            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Forecast script not found: {scriptPath}");
            }

            _logger.LogInformation("Starting forecast script: {ScriptPath}", scriptPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExecutable,
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? AppContext.BaseDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.Environment["TZ"] = "Europe/Kyiv";

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            var stdOut = await stdOutTask;
            var stdErr = await stdErrTask;

            if (!string.IsNullOrWhiteSpace(stdOut))
            {
                _logger.LogInformation("Forecast script output: {Output}", stdOut);
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Forecast script failed with code {process.ExitCode}. Error: {stdErr}");
            }

            if (!string.IsNullOrWhiteSpace(stdErr))
            {
                _logger.LogWarning("Forecast script warnings: {Error}", stdErr);
            }

            _logger.LogInformation("Forecast script completed successfully.");
        }

        public async Task CleanupOldForecastsAsync(CancellationToken cancellationToken = default)
        {
            var now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Kyiv");
            var threeMonthsAgo = now.AddMonths(-3);

            await _forecastRepository.DeleteOlderThanAsync(threeMonthsAgo, cancellationToken);
        }

        private static string ResolveScriptPath(string? configuredPath)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return Path.GetFullPath(configuredPath);
            }

            return Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "Forecast",
                "Forecast.py"));
        }
    }
}
