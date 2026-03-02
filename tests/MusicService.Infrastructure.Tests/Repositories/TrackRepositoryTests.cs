using FluentAssertions;
using MusicService.Application.Tracks.Queries;
using MusicService.Domain.Entities;
using Tests.EFCoreTests;
using Xunit;

namespace Tests.MusicService.Infrastructure.Tests.Repositories;

public class TrackRepositoryTests
{
    [Fact]
    public async Task GetTracksByAlbum_ShouldFilterAndOrderByTrackNumber()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var artistId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        var otherAlbumId = Guid.NewGuid();
        dbContext.Artists.Add(new Artist
        {
            Id = artistId,
            Name = "Artist",
            Country = "US",
            Genres = new List<string>()
        });
        dbContext.Albums.AddRange(
            new Album
            {
                Id = albumId,
                ArtistId = artistId,
                Title = "Album",
                ReleaseDate = DateTime.UtcNow,
                Type = AlbumType.Album,
                Genres = new List<string>()
            },
            new Album
            {
                Id = otherAlbumId,
                ArtistId = artistId,
                Title = "Other Album",
                ReleaseDate = DateTime.UtcNow,
                Type = AlbumType.Album,
                Genres = new List<string>()
            }
        );
        dbContext.Tracks.AddRange(
            new Track { Id = Guid.NewGuid(), Title = "Second", TrackNumber = 2, AlbumId = albumId, ArtistId = artistId, DurationSeconds = 100 },
            new Track { Id = Guid.NewGuid(), Title = "First", TrackNumber = 1, AlbumId = albumId, ArtistId = artistId, DurationSeconds = 100 },
            new Track { Id = Guid.NewGuid(), Title = "Other", TrackNumber = 3, AlbumId = otherAlbumId, ArtistId = artistId, DurationSeconds = 100 }
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetTracksByAlbumQueryHandler(dbContext, TestMapperFactory.Create());

        var result = await handler.Handle(new GetTracksByAlbumQuery { AlbumId = albumId, SortByTrackNumber = true }, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(t => t.Title).Should().Equal("First", "Second");
    }
}
