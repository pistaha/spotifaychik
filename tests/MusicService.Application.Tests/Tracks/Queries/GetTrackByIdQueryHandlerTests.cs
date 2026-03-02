using FluentAssertions;
using MusicService.Application.Tracks.Queries;
using MusicService.Domain.Entities;
using Tests.EFCoreTests;
using Xunit;

namespace Tests.MusicService.Application.Tests.Tracks.Queries;

public class GetTrackByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnTrackDto_WhenTrackExists()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var artistId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        dbContext.Artists.Add(new Artist
        {
            Id = artistId,
            Name = "Artist",
            Country = "US",
            Genres = new List<string>()
        });
        dbContext.Albums.Add(new Album
        {
            Id = albumId,
            ArtistId = artistId,
            Title = "Album",
            ReleaseDate = DateTime.UtcNow,
            Type = AlbumType.Album,
            Genres = new List<string>()
        });
        dbContext.Tracks.Add(new Track
        {
            Id = trackId,
            Title = "Test Track",
            DurationSeconds = 180,
            TrackNumber = 1,
            AlbumId = albumId,
            ArtistId = artistId
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetTrackByIdQueryHandler(dbContext, TestMapperFactory.Create());

        var result = await handler.Handle(new GetTrackByIdQuery { TrackId = trackId }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(trackId);
        result.Title.Should().Be("Test Track");
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenTrackNotFound()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var handler = new GetTrackByIdQueryHandler(dbContext, TestMapperFactory.Create());

        var result = await handler.Handle(new GetTrackByIdQuery { TrackId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeNull();
    }
}
