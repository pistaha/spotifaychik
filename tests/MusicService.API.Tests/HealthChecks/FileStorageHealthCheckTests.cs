using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MusicService.API.Configuration;
using Moq;
using Xunit;

namespace Tests.MusicService.API.Tests.HealthChecks;

public class FileStorageHealthCheckTests
{
    [Fact]
    public void AddApiServices_ShouldRegisterDatabaseHealthCheck()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:EnableDevelopmentAuth"] = "true"
            })
            .Build();
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(Environments.Development);

        services.AddApiServices(configuration, env.Object);

        using var provider = services.BuildServiceProvider();
        var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        healthOptions.Registrations.Should().Contain(r => r.Name == "database");
    }
}
