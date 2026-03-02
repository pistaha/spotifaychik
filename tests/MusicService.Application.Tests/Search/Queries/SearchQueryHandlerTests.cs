using FluentAssertions;
using Microsoft.Extensions.Logging;
using MusicService.Application.Search.Queries;
using Tests.EFCoreTests;
using Xunit;

namespace Tests.MusicService.Application.Tests.Search.Queries;

public class SearchQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmptyResult_WhenTypeIsUnknown()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var handler = new SearchQueryHandler(
            dbContext,
            LoggerFactory.Create(_ => { }).CreateLogger<SearchQueryHandler>());

        var result = await handler.Handle(new SearchQuery { Query = "Rock", Type = "unknown", Limit = 5 }, CancellationToken.None);

        result.TotalResults.Should().Be(0);
        result.Artists.Should().BeEmpty();
        result.Albums.Should().BeEmpty();
        result.Tracks.Should().BeEmpty();
        result.Playlists.Should().BeEmpty();
        result.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyResult_WhenProviderDoesNotSupportSearch()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var handler = new SearchQueryHandler(
            dbContext,
            LoggerFactory.Create(_ => { }).CreateLogger<SearchQueryHandler>());

        var result = await handler.Handle(new SearchQuery { Query = "Rock", Type = "all", Limit = 5 }, CancellationToken.None);

        result.TotalResults.Should().Be(0);
    }
}
