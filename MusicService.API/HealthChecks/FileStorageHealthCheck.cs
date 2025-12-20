using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MusicService.Infrastructure.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.API.HealthChecks
{
    public class FileStorageHealthCheck : IHealthCheck
    {
        private readonly FileStorageOptions _options;

        public FileStorageHealthCheck(IOptions<FileStorageOptions> options)
        {
            _options = options.Value;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Проверяем существование директории данных
                if (!Directory.Exists(_options.DataDirectory))
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        $"Data directory '{_options.DataDirectory}' does not exist"));
                }

                // Проверяем права на запись
                var testFilePath = Path.Combine(_options.DataDirectory, $"healthcheck_{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFilePath, "healthcheck");
                File.Delete(testFilePath);

                // Проверяем свободное место на диске
                var driveInfo = new DriveInfo(Path.GetPathRoot(_options.DataDirectory) ?? "/");
                var freeSpacePercent = (double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize * 100;

                if (freeSpacePercent < 10) // Менее 10% свободного места
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Low disk space: {freeSpacePercent:F1}% free"));
                }

                return Task.FromResult(HealthCheckResult.Healthy(
                    $"File storage is healthy. Free space: {freeSpacePercent:F1}%"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "File storage health check failed", ex));
            }
        }
    }
}