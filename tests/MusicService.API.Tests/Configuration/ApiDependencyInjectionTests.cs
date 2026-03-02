using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MusicService.API.Configuration;
using Moq;
using Xunit;

namespace Tests.MusicService.API.Tests.Configuration;

public class ApiDependencyInjectionTests
{
    [Fact]
    public void AddApiServices_ShouldConfigureMvcBehaviorAndJson()
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
        var apiBehavior = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
        apiBehavior.SuppressModelStateInvalidFilter.Should().BeTrue();
        apiBehavior.InvalidModelStateResponseFactory.Should().NotBeNull();
    }

    [Fact]
    public void AddApiServices_ShouldRegisterHealthChecksAndCorsPolicies()
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

        var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        corsOptions.GetPolicy("AllowAll").Should().NotBeNull();
        corsOptions.GetPolicy("ProductionCors").Should().NotBeNull();
    }
}
