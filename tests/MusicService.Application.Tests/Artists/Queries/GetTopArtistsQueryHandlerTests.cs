using FluentAssertions;
using MusicService.Application.Artists.Queries;
using MusicService.Domain.Entities;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Artists.Queries;

public class GetTopArtistsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnMappedArtists()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        dbContext.Artists.Add(new Artist
        {
            Id = Guid.NewGuid(),
            Name = "Top",
            Country = "US",
            MonthlyListeners = 100,
            Genres = new List<string>()
        });
        dbContext.Artists.Add(new Artist
        {
            Id = Guid.NewGuid(),
            Name = "Low",
            Country = "US",
            MonthlyListeners = 10,
            Genres = new List<string>()
        });
        await dbContext.SaveChangesAsync();
        var handler = new GetTopArtistsQueryHandler(dbContext);

        var result = await handler.Handle(new GetTopArtistsQuery { Count = 3 }, CancellationToken.None);

        result.Should().ContainSingle(a => a.Name == "Top");
    }
}
