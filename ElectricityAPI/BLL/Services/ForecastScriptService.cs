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
            string pythonExecutable = _configuration["ForecastScript:PythonExecutable"] ?? "python";
            string? configuredPath = _configuration["ForecastScript:ScriptPath"];
            string scriptPath = ResolveScriptPath(configuredPath);

            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Forecast script not found: {scriptPath}");
            }

            _logger.LogInformation("Starting forecast script: {ScriptPath}", scriptPath);

            ProcessStartInfo startInfo = new ProcessStartInfo
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

            using Process process = new Process { StartInfo = startInfo };
            process.Start();

            Task<string> stdOutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stdErrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            string stdOut = await stdOutTask;
            string stdErr = await stdErrTask;

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
            DateTime now = KyivTimeHelper.Now;
            DateTime threeMonthsAgo = now.AddMonths(-3);

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
