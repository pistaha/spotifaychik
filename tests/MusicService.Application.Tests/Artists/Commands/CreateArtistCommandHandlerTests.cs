using FluentAssertions;
using MusicService.Application.Artists.Commands;
using MusicService.Application.Artists.Dtos;
using Microsoft.Extensions.Logging;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Artists.Commands;

public class CreateArtistCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateArtistAndReturnDto()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var handler = new CreateArtistCommandHandler(
            dbContext,
            TestMapperFactory.Create(),
            LoggerFactory.Create(_ => { }).CreateLogger<CreateArtistCommandHandler>());

        var result = await handler.Handle(new CreateArtistCommand { Name = "Artist" }, CancellationToken.None);

        result.Should().BeOfType<ArtistDto>();
        dbContext.Artists.Should().ContainSingle(a => a.Name == "Artist");
    }
}
