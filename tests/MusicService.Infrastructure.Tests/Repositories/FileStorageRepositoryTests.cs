using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MusicService.Infrastructure.Configuration;
using Xunit;

namespace Tests.MusicService.Infrastructure.Tests.Repositories;

public class FileStorageRepositoryTests
{
    [Fact]
    public void AddInfrastructure_ShouldThrow_WhenConnectionStringMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var act = () => services.AddInfrastructure(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Database:ConnectionString*");
    }
}
