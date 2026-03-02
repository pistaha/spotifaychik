using FluentAssertions;
using MusicService.Application.Playlists.Queries;
using MusicService.Domain.Entities;
using Tests.EFCoreTests;
using Xunit;

namespace Tests.MusicService.Infrastructure.Tests.Repositories;

public class PlaylistRepositoryTests
{
    [Fact]
    public async Task GetPublicPlaylists_ShouldReturnOnlyPublic()
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
        dbContext.Playlists.AddRange(
            new Playlist { Id = Guid.NewGuid(), CreatedById = userId, Title = "Public", IsPublic = true, Type = PlaylistType.UserCreated },
            new Playlist { Id = Guid.NewGuid(), CreatedById = userId, Title = "Private", IsPublic = false, Type = PlaylistType.UserCreated }
        );
        await dbContext.SaveChangesAsync();

        var handler = new GetPublicPlaylistsQueryHandler(dbContext);

        var result = await handler.Handle(new GetPublicPlaylistsQuery { SortBy = "title", SortOrder = "asc" }, CancellationToken.None);

        result.Should().ContainSingle(p => p.Title == "Public");
        result.Should().NotContain(p => p.Title == "Private");
    }
}
