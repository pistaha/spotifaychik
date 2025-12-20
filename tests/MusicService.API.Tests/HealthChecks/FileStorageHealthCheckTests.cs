using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MusicService.API.HealthChecks;
using MusicService.Infrastructure.Configuration;
using Xunit;

namespace Tests.MusicService.API.Tests.HealthChecks;

public class FileStorageHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenDirectoryExistsAndWritable()
    {
        var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"health_{Guid.NewGuid():N}")).FullName;
        var options = Options.Create(new FileStorageOptions { DataDirectory = tempDir });
        var healthCheck = new FileStorageHealthCheck(options);

        try
        {
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            result.Status.Should().Be(HealthStatus.Healthy);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenDirectoryMissing()
    {
        var missingDir = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}");
        var options = Options.Create(new FileStorageOptions { DataDirectory = missingDir });
        var healthCheck = new FileStorageHealthCheck(options);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("does not exist");
    }
}
