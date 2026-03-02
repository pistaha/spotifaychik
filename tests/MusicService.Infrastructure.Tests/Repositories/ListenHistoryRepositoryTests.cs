using FluentAssertions;
using MusicService.Domain.Entities;
using Tests.EFCoreTests;
using Xunit;

namespace Tests.MusicService.Infrastructure.Tests.Repositories;

public class ListenHistoryRepositoryTests
{
    [Fact]
    public async Task ListenHistory_ShouldPersistAndQueryByUser()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();
        var artistId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
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
        dbContext.Tracks.Add(new Track
        {
            Id = trackId,
            Title = "Track",
            DurationSeconds = 180,
            TrackNumber = 1,
            AlbumId = albumId,
            ArtistId = artistId
        });
        dbContext.ListenHistories.Add(new ListenHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TrackId = trackId,
            ListenedAt = DateTime.UtcNow,
            ListenDurationSeconds = 180
        });
        await dbContext.SaveChangesAsync();

        var history = dbContext.ListenHistories.Where(h => h.UserId == userId).ToList();

        history.Should().HaveCount(1);
        history.Single().TrackId.Should().Be(trackId);
    }
}
