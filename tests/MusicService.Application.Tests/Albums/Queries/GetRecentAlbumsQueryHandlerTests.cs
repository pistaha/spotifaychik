using FluentAssertions;
using MusicService.Application.Albums.Queries;
using MusicService.Domain.Entities;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Albums.Queries;

public class GetRecentAlbumsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldRequestRecentAlbumsForGivenDays()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var artist = new Artist
        {
            Id = Guid.NewGuid(),
            Name = "Artist",
            Country = "US",
            Genres = new List<string>()
        };
        dbContext.Artists.Add(artist);
        dbContext.Albums.Add(new Album
        {
            Id = Guid.NewGuid(),
            ArtistId = artist.Id,
            Artist = artist,
            Title = "Recent",
            ReleaseDate = DateTime.UtcNow.AddDays(-1),
            Type = AlbumType.Album,
            Genres = new List<string>()
        });
        await dbContext.SaveChangesAsync();
        var handler = new GetRecentAlbumsQueryHandler(dbContext, TestMapperFactory.Create());

        var result = await handler.Handle(new GetRecentAlbumsQuery { Days = 7 }, CancellationToken.None);

        result.Should().HaveCount(1);
    }
}
