using FluentAssertions;
using MusicService.Application.Tracks.Queries;
using MusicService.Domain.Entities;
using Tests.EFCoreTests;
using Xunit;

namespace Tests.MusicService.Application.Tests.Tracks.Queries;

public class GetTopTracksQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUseListenHistory_WhenTimeRangeSpecified()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();
        var artistId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        var trackAId = Guid.NewGuid();
        var trackBId = Guid.NewGuid();
        dbContext.Users.Add(new User
        {
            Id = userId,
            Username = "user",
            Email = "user@music.local",
            PasswordHash = "hash",
            DisplayName = "User",
            Country = "US",
            FavoriteGenres = new List<string>()
        });
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
        dbContext.Tracks.AddRange(
            new Track
            {
                Id = trackAId,
                Title = "Track A",
                DurationSeconds = 180,
                TrackNumber = 1,
                AlbumId = albumId,
                ArtistId = artistId
            },
            new Track
            {
                Id = trackBId,
                Title = "Track B",
                DurationSeconds = 180,
                TrackNumber = 2,
                AlbumId = albumId,
                ArtistId = artistId
            });
        dbContext.ListenHistories.AddRange(
            new ListenHistory { Id = Guid.NewGuid(), TrackId = trackAId, UserId = userId, ListenedAt = DateTime.UtcNow.AddDays(-1), ListenDurationSeconds = 180 },
            new ListenHistory { Id = Guid.NewGuid(), TrackId = trackAId, UserId = userId, ListenedAt = DateTime.UtcNow.AddDays(-2), ListenDurationSeconds = 180 },
            new ListenHistory { Id = Guid.NewGuid(), TrackId = trackBId, UserId = userId, ListenedAt = DateTime.UtcNow.AddDays(-1), ListenDurationSeconds = 180 }
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetTopTracksQueryHandler(dbContext);

        var result = await handler.Handle(new GetTopTracksQuery { Count = 5, TimeRange = "week" }, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(t => t.Title).Should().Equal("Track A", "Track B");
    }

    [Fact]
    public async Task Handle_ShouldOrderByPlayCount_WhenTimeRangeIsAll()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var artistId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
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
        dbContext.Tracks.AddRange(
            new Track
            {
                Id = Guid.NewGuid(),
                Title = "Alpha",
                DurationSeconds = 180,
                TrackNumber = 1,
                PlayCount = 100,
                AlbumId = albumId,
                ArtistId = artistId
            },
            new Track
            {
                Id = Guid.NewGuid(),
                Title = "Beta",
                DurationSeconds = 180,
                TrackNumber = 2,
                PlayCount = 50,
                AlbumId = albumId,
                ArtistId = artistId
            });
        await dbContext.SaveChangesAsync();

        var handler = new GetTopTracksQueryHandler(dbContext);

        var result = await handler.Handle(new GetTopTracksQuery { Count = 2, TimeRange = "all" }, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(t => t.Title).Should().Equal("Alpha", "Beta");
    }
}
