using FluentAssertions;
using Microsoft.Extensions.Logging;
using MusicService.Application.Users.Queries;
using MusicService.Domain.Entities;
using Tests.EFCoreTests;
using Xunit;

namespace Tests.MusicService.Application.Tests.Users.Queries;

public class GetUserStatisticsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmptyStatistics_WhenUserNotFound()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var handler = new GetUserStatisticsQueryHandler(
            dbContext,
            LoggerFactory.Create(_ => { }).CreateLogger<GetUserStatisticsQueryHandler>());

        var result = await handler.Handle(new GetUserStatisticsQuery { UserId = Guid.NewGuid() }, CancellationToken.None);

        result.TotalPlaylists.Should().Be(0);
        result.TopTracks.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldAggregateListeningStats()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        var artistId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "user",
            Email = "user@music.local",
            PasswordHash = "hash",
            DisplayName = "User",
            Country = "US",
            FavoriteGenres = new List<string>()
        };
        var friend = new User
        {
            Id = friendId,
            Username = "friend",
            Email = "friend@music.local",
            PasswordHash = "hash",
            DisplayName = "Friend",
            Country = "US",
            FavoriteGenres = new List<string>()
        };
        var artist = new Artist
        {
            Id = artistId,
            Name = "Artist",
            Country = "US",
            Genres = new List<string> { "Rock" }
        };
        var album = new Album
        {
            Id = albumId,
            ArtistId = artistId,
            Title = "Album",
            ReleaseDate = DateTime.UtcNow,
            Type = AlbumType.Album,
            Genres = new List<string> { "Rock" }
        };
        var track = new Track
        {
            Id = trackId,
            Title = "Track",
            DurationSeconds = 180,
            TrackNumber = 1,
            AlbumId = albumId,
            ArtistId = artistId
        };
        var playlist = new Playlist
        {
            Id = Guid.NewGuid(),
            Title = "Playlist",
            CreatedById = userId,
            IsPublic = true,
            Type = PlaylistType.UserCreated
        };
        var listenHistory = new ListenHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TrackId = trackId,
            ListenedAt = DateTime.UtcNow.AddDays(-1),
            ListenDurationSeconds = 180
        };

        user.Friends.Add(friend);
        user.FollowedArtists.Add(artist);
        user.FollowedPlaylists.Add(playlist);
        dbContext.Users.AddRange(user, friend);
        dbContext.Artists.Add(artist);
        dbContext.Albums.Add(album);
        dbContext.Tracks.Add(track);
        dbContext.Playlists.Add(playlist);
        dbContext.ListenHistories.Add(listenHistory);
        await dbContext.SaveChangesAsync();

        var handler = new GetUserStatisticsQueryHandler(
            dbContext,
            LoggerFactory.Create(_ => { }).CreateLogger<GetUserStatisticsQueryHandler>());

        var result = await handler.Handle(new GetUserStatisticsQuery { UserId = userId }, CancellationToken.None);

        result.TotalPlaylists.Should().Be(1);
        result.TotalListeningTime.Should().BeGreaterThan(0);
        result.TopTracks.Should().ContainSingle();
        result.TopArtists.Should().ContainSingle();
        result.TopGenres.Should().Contain("Rock");
        result.FollowersCount.Should().Be(1);
        result.FollowingCount.Should().Be(2);
    }
}
