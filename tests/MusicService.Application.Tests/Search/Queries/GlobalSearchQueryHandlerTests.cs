using FluentAssertions;
using Microsoft.Extensions.Logging;
using MusicService.Application.Search.Queries;
using Tests.EFCoreTests;
using Xunit;

namespace Tests.MusicService.Application.Tests.Search.Queries;

public class GlobalSearchQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmptyResult_WhenProviderDoesNotSupportSearch()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var handler = new GlobalSearchQueryHandler(
            dbContext,
            LoggerFactory.Create(_ => { }).CreateLogger<GlobalSearchQueryHandler>());

        var result = await handler.Handle(new GlobalSearchQuery { Query = "mix", Limit = 1 }, CancellationToken.None);

        result.TotalResults.Should().Be(0);
        result.TopArtists.Should().BeEmpty();
        result.TopAlbums.Should().BeEmpty();
        result.TopTracks.Should().BeEmpty();
        result.TopPlaylists.Should().BeEmpty();
    }
}
