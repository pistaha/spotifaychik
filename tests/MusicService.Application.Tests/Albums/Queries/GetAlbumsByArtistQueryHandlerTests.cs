using FluentAssertions;
using MusicService.Application.Albums.Queries;
using MusicService.Domain.Entities;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Albums.Queries;

public class GetAlbumsByArtistQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnAlbumsByArtist()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var artistId = Guid.NewGuid();
        var artist = new Artist
        {
            Id = artistId,
            Name = "Artist",
            Country = "US",
            Genres = new List<string>()
        };
        dbContext.Artists.Add(artist);
        dbContext.Albums.Add(new Album
        {
            Id = Guid.NewGuid(),
            ArtistId = artistId,
            Artist = artist,
            Title = "Album",
            ReleaseDate = DateTime.UtcNow,
            Type = AlbumType.Album,
            Genres = new List<string>()
        });
        await dbContext.SaveChangesAsync();
        var handler = new GetAlbumsByArtistQueryHandler(dbContext, TestMapperFactory.Create());

        var result = await handler.Handle(new GetAlbumsByArtistQuery { ArtistId = artistId }, CancellationToken.None);

        result.Should().HaveCount(1);
    }
}
