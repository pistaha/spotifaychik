using FluentAssertions;
using MusicService.Application.Albums.Queries;
using MusicService.Domain.Entities;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Albums.Queries;

public class GetAlbumByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNull_WhenNotFound()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var handler = new GetAlbumByIdQueryHandler(dbContext, TestMapperFactory.Create());

        var result = await handler.Handle(new GetAlbumByIdQuery { AlbumId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldMapAlbum()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var artist = new Artist
        {
            Id = Guid.NewGuid(),
            Name = "Artist",
            Country = "US",
            Genres = new List<string>()
        };
        var album = new Album
        {
            Id = Guid.NewGuid(),
            Title = "T",
            ArtistId = artist.Id,
            Artist = artist,
            ReleaseDate = DateTime.UtcNow,
            Type = AlbumType.Album,
            Genres = new List<string>()
        };
        dbContext.Artists.Add(artist);
        dbContext.Albums.Add(album);
        await dbContext.SaveChangesAsync();
        var handler = new GetAlbumByIdQueryHandler(dbContext, TestMapperFactory.Create());

        var result = await handler.Handle(new GetAlbumByIdQuery { AlbumId = album.Id }, CancellationToken.None);

        result!.Id.Should().Be(album.Id);
    }
}
