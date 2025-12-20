using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MusicService.API.Configuration;
using MusicService.API.HealthChecks;
using Xunit;

namespace Tests.MusicService.API.Tests.Configuration;

public class ApiDependencyInjectionTests
{
    [Fact]
    public void AddApiServices_ShouldConfigureMvcBehaviorAndJson()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddApiServices(configuration);

        using var provider = services.BuildServiceProvider();
        var apiBehavior = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
        apiBehavior.SuppressModelStateInvalidFilter.Should().BeTrue();
        apiBehavior.InvalidModelStateResponseFactory.Should().NotBeNull();
    }

    [Fact]
    public void AddApiServices_ShouldRegisterHealthChecksAndCorsPolicies()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddApiServices(configuration);

        using var provider = services.BuildServiceProvider();
        var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        healthOptions.Registrations.Should().Contain(r => r.Name == "file_storage" && r.Factory.Invoke(provider) is FileStorageHealthCheck);

        var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        corsOptions.GetPolicy("AllowAll").Should().NotBeNull();
        corsOptions.GetPolicy("ProductionCors").Should().NotBeNull();
    }
}
