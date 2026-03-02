using FluentAssertions;
using MusicService.Application.AI.Queries;
using MusicService.Application.Playlists.Dtos;
using MusicService.Domain.Entities;
using Tests.EFCoreTests;
using Xunit;

namespace Tests.MusicService.Application.Tests.AI.Queries;

public class GeneratePersonalPlaylistsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenUserNotFound()
    {
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var handler = new GeneratePersonalPlaylistsQueryHandler(dbContext);
        var query = new GeneratePersonalPlaylistsQuery { UserId = Guid.NewGuid(), Count = 3 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldGeneratePlaylists_ForFavoriteGenresUpToRequestedCount()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "user",
            Email = "user@music.local",
            PasswordHash = "hash",
            DisplayName = "User",
            Country = "US",
            FavoriteGenres = new List<string> { "Rock", "Pop", "Indie" }
        };
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = new GeneratePersonalPlaylistsQueryHandler(dbContext);
        var query = new GeneratePersonalPlaylistsQuery { UserId = user.Id, Count = 2 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(p => p.Title).Should().Contain("Rock Classics Mix").And.Contain("Top Pop Hits");
        result.All(p => p.CreatedById == user.Id && p.Type == "SystemGenerated").Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldUseFallbackTitle_ForUnknownGenres()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "user",
            Email = "user@music.local",
            PasswordHash = "hash",
            DisplayName = "User",
            Country = "US",
            FavoriteGenres = new List<string> { "Folk" }
        };
        using var dbContext = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = new GeneratePersonalPlaylistsQueryHandler(dbContext);

        var result = await handler.Handle(new GeneratePersonalPlaylistsQuery
        {
            UserId = user.Id,
            Count = 5
        }, CancellationToken.None);

        result.Should().ContainSingle();
        var playlist = result.Single();
        playlist.Title.Should().Be("My Folk Mix");
        playlist.Description.Should().NotBeNull();
        playlist.Description!.Should().ContainEquivalentOf("Folk");
        playlist.Should().BeOfType<PlaylistDto>();
    }
}
