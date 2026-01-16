using FluentAssertions;
using MusicService.Application.Playlists.Queries;
using MusicService.Domain.Entities;
using Xunit;
using Tests.EFCoreTests;

namespace Tests.MusicService.Application.Tests.Playlists.Queries;

public class GetUserPlaylistsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldFilterPrivatePlaylists_WhenIncludePrivateIsFalse()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();
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
        var playlists = new List<Playlist>
        {
            new() { Id = Guid.NewGuid(), CreatedById = userId, IsPublic = true, PlaylistTracks = new List<PlaylistTrack>(), Title = "Public", Type = PlaylistType.UserCreated },
            new() { Id = Guid.NewGuid(), CreatedById = userId, IsPublic = false, PlaylistTracks = new List<PlaylistTrack>(), Title = "Private", Type = PlaylistType.UserCreated }
        };
        dbContext.Playlists.AddRange(playlists);
        await dbContext.SaveChangesAsync();
        var handler = new GetUserPlaylistsQueryHandler(dbContext);

        var result = await handler.Handle(new GetUserPlaylistsQuery { UserId = userId, IncludePrivate = false }, CancellationToken.None);

        result.Should().HaveCount(1);
        result.All(p => p.IsPublic).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnAllPlaylists_WhenIncludePrivateIsTrue()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();
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
        dbContext.Playlists.Add(new Playlist
        {
            Id = Guid.NewGuid(),
            CreatedById = userId,
            IsPublic = false,
            PlaylistTracks = new List<PlaylistTrack>(),
            Title = "Private",
            Type = PlaylistType.UserCreated
        });
        await dbContext.SaveChangesAsync();
        var handler = new GetUserPlaylistsQueryHandler(dbContext);

        var result = await handler.Handle(new GetUserPlaylistsQuery { UserId = userId, IncludePrivate = true }, CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().IsPublic.Should().BeFalse();
    }
}
