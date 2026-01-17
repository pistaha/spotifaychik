using FluentAssertions;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Playlists.Queries;
using MusicService.Domain.Entities;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Playlists.Queries;

public class GetPlaylistByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNull_WhenPlaylistNotFound()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var handler = new GetPlaylistByIdQueryHandler(dbContext, TestMapperFactory.Create());
        var query = new GetPlaylistByIdQuery { PlaylistId = Guid.NewGuid() };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldSetIsFollowingFlag_WhenUserIsFollower()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var followerId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var follower = new User
        {
            Id = followerId,
            Username = "follower",
            Email = "follower@music.local",
            PasswordHash = "hash",
            DisplayName = "Follower",
            Country = "US",
            FavoriteGenres = new List<string>()
        };
        var creator = new User
        {
            Id = creatorId,
            Username = "creator",
            Email = "creator@music.local",
            PasswordHash = "hash",
            DisplayName = "Creator",
            Country = "US",
            FavoriteGenres = new List<string>()
        };
        var playlist = new Playlist
        {
            Id = Guid.NewGuid(),
            Title = "Chill",
            CreatedById = creatorId,
            IsPublic = true,
            Followers = new List<User> { follower },
            PlaylistTracks = new List<PlaylistTrack>()
        };
        dbContext.Users.AddRange(follower, creator);
        dbContext.Playlists.Add(playlist);
        await dbContext.SaveChangesAsync();
        var handler = new GetPlaylistByIdQueryHandler(dbContext, TestMapperFactory.Create());

        var result = await handler.Handle(new GetPlaylistByIdQuery { PlaylistId = playlist.Id, UserId = followerId }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsFollowing.Should().BeTrue();
        result.Should().BeOfType<PlaylistDto>();
    }
}
